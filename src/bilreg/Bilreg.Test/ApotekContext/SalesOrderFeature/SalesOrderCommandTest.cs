using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.SalesOrderFeature;

public class SalesOrderCommandTest
{
    private readonly InMemoryResepRepo _resep = new();
    private readonly InMemoryTelaahRepo _telaah = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly InMemoryJualBebasRepo _jb = new();
    private readonly InMemoryCopyResepRepo _copy = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
    private readonly InMemoryInvoiceRepo _inv = new();
    private readonly FakePricePort _price = new();
    private readonly AllowAllAuth _auth = new();

    [Fact]
    public async Task Patient_request_exclusion_issues_copy_resep_with_correct_reason_and_qty()
    {
        var resepId = await IntakeAndApproveTwoLines();
        var handler = CreateHandler(DeterministicAvailableStockPort.Full());

        var response = await handler.Handle(new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.Resep, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.PatientRequest, [
                new SalesOrderEstablishItem(1, 5),
                new SalesOrderEstablishItem(2, 0)
            ]), default);

        var stored = _so.Store.Values.Single();
        stored.PartialReason.Should().Be(PartialReasonEnum.PatientRequest);
        stored.Items.Should().ContainSingle();
        stored.Items[0].AcceptedQty.Should().Be(5m);
        response.CopyResepId.Should().NotBeNullOrWhiteSpace();

        var copy = _copy.Store.Values.Single();
        copy.Reason.Should().Be((int)PartialReasonEnum.PatientRequest);
        copy.Items.Should().ContainSingle();
        copy.Items[0].ResepItemNo.Should().Be(2);
        copy.Items[0].Qty.Should().Be(6m);
    }

    [Fact]
    public async Task Establish_rejects_patient_request_reason_when_stock_shortage_drives_exclusion()
    {
        var resepId = await IntakeAndApprove();
        var handler = CreateHandler(DeterministicAvailableStockPort.Partial(4));

        var act = () => handler.Handle(new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.Resep, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.PatientRequest, null), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*does not match exclusion cause 'StockShortage'*");
        _so.Store.Should().BeEmpty();
        _copy.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Establish_with_iter_entitled_enqueues_iter_consume_task()
    {
        var resepId = await IntakeAndApprove();
        var handler = CreateHandler(DeterministicAvailableStockPort.Full());

        var response = await handler.Handle(new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.Resep, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);

        _tasks.Store.Should().ContainSingle();
        var task = _tasks.Store.Values.Single();
        task.TaskType.Should().Be(AptIntegrationTaskTypeEnum.IterConsume);
        task.Destination.Should().Be(AptIntegrationDestinationEnum.ResepIter);
        task.IdempotencyKey.Should().Be($"{response.SalesOrderId}:ITER");
        task.SourceId.Should().Be(response.SalesOrderId);
        task.PayloadJson.Should().Contain("RS-1");
        task.PayloadJson.Should().Contain("\"ConsumeCount\":1");
    }

    [Fact]
    public async Task Establish_without_iter_entitled_skips_iter_consume_task()
    {
        var resep = SeedResep("RS-NO-ITER", 0, ("A", "A", 10m));
        var resepId = await CompleteTelaah(resep.ResepId, 1, 10);
        var handler = CreateHandler(DeterministicAvailableStockPort.Full());

        await handler.Handle(new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.Resep, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);

        _tasks.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Duplicate_establish_does_not_enqueue_second_iter_task()
    {
        var resepId = await IntakeAndApprove();
        var handler = CreateHandler(DeterministicAvailableStockPort.Full());
        var cmd = new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.Resep, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null);

        var first = await handler.Handle(cmd, default);
        var second = await handler.Handle(cmd, default);

        second.SalesOrderId.Should().Be(first.SalesOrderId);
        _tasks.Store.Should().ContainSingle();
        _tasks.Store.Values.Single().IdempotencyKey.Should().Be($"{first.SalesOrderId}:ITER");
    }

    [Fact]
    public async Task Post_establishment_shortage_appends_outcome_and_copy_resep_without_trimming_accepted_qty()
    {
        var order = SeedEstablishedSalesOrder();
        var handler = CreateUnfulfilledHandler(new DenyTrPermission());

        var response = await handler.Handle(new SalesOrderAppendUnfulfilledCmd(
            "u", order.SalesOrderId, order.Version, 1, 3m, UnfulfilledReasonEnum.StockShortageAfterEstablishment), default);

        var stored = _so.Store[order.SalesOrderId];
        stored.Item(1).AcceptedQty.Should().Be(10m);
        stored.Item(1).BrgId.Should().Be("BRG1");
        stored.Item(1).UnfulfilledQty.Should().Be(3m);
        stored.Outcomes.Should().ContainSingle(x =>
            x.Qty == 3m
            && x.Reason == UnfulfilledReasonEnum.StockShortageAfterEstablishment
            && x.CopyResepId == response.CopyResepId
            && x.ActorId == "u");
        response.CopyResepId.Should().NotBeNullOrWhiteSpace();
        response.InvoiceCorrection.Should().BeNull();

        var copy = _copy.Store[response.CopyResepId!];
        copy.Reason.Should().Be((int)PartialReasonEnum.StockShortage);
        copy.Items.Should().ContainSingle(x => x.Qty == 3m && x.BrgId == "BRG1");
    }

    [Fact]
    public async Task Post_establishment_shortage_routes_manual_correction_when_issued_invoice_locked()
    {
        var order = SeedEstablishedSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);
        _inv.Store[issued.InvoiceId].RecordChargeCorrelation("TRC1");
        var currentOrder = _so.Store[order.SalesOrderId];

        var response = await CreateUnfulfilledHandler(new DenyTrPermission()).Handle(
            new SalesOrderAppendUnfulfilledCmd(
                "u", currentOrder.SalesOrderId, currentOrder.Version, 1, 2m,
                UnfulfilledReasonEnum.StockShortageAfterEstablishment),
            default);

        response.InvoiceCorrection.Should().NotBeNull();
        response.InvoiceCorrection!.InvoiceId.Should().Be(issued.InvoiceId);
        response.InvoiceCorrection.CorrectionDisposition
            .Should().Be(InvoiceCorrectionDispositionEnum.ManualTataRekeningCorrectionPending);
        response.InvoiceCorrection.RequiresManualTataRekeningCorrection.Should().BeTrue();
        response.InvoiceCorrection.AllowsDirectRevision.Should().BeFalse();
    }

    [Fact]
    public async Task Post_establishment_shortage_routes_direct_revision_when_permission_allows()
    {
        var order = SeedEstablishedSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);
        var currentOrder = _so.Store[order.SalesOrderId];

        var response = await CreateUnfulfilledHandler(new AllowTrPermission()).Handle(
            new SalesOrderAppendUnfulfilledCmd(
                "u", currentOrder.SalesOrderId, currentOrder.Version, 1, 2m,
                UnfulfilledReasonEnum.StockShortageAfterEstablishment),
            default);

        response.InvoiceCorrection.Should().NotBeNull();
        response.InvoiceCorrection!.CorrectionDisposition
            .Should().Be(InvoiceCorrectionDispositionEnum.DirectRevisionAllowed);
        response.InvoiceCorrection.AllowsDirectRevision.Should().BeTrue();
        response.InvoiceCorrection.RequiresManualTataRekeningCorrection.Should().BeFalse();
    }

    private SalesOrderAppendUnfulfilledHandler CreateUnfulfilledHandler(ITataRekeningInvoicePermissionPort permission)
        => new(_so, _copy, _inv, permission, _auth);

    private SalesOrderModel SeedEstablishedSalesOrder()
    {
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ1", "", "R1", "P1", "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat", "TAB", 10, FornasCoverageEnum.Unknown, "", false)],
            []);
        _so.SaveChanges(order);
        return order;
    }

    private SalesOrderEstablishHandler CreateHandler(IAvailableStockPort stock)
        => new(_so, _telaah, _resep, _jb, stock, _copy, _tasks, _auth);

    private ResepModel SeedResep(string resepId, int iter, params (string brgId, string brgName, decimal qty)[] items)
    {
        var regReff = new RegReff("R1", "P1", "Pasien");
        var resep = new ResepModel(
            resepId, regReff, BodyMetricType.Default(), DokterType.Default.ToReff(),
            LayananType.Default.ToReff(), LayananType.Default.ToReff(),
            UrgenitasType.Default, TipeBrgType.Default.ToReff(), iter, "desc",
            DateTime.Today, resepId, AppConst.DASH, AuditTrailType.Default, []);
        foreach (var (brgId, brgName, qty) in items)
        {
            var brg = BrgObatType.Default with { BrgId = brgId, BrgName = brgName };
            resep.AddObat(brg, SatuanType.Default, qty, 0, "3x1", "", "");
        }
        _resep.SaveChanges(resep);
        return resep;
    }

    private async Task<string> IntakeAndApprove()
    {
        var resep = SeedResep("RS-1", 1, ("A", "A", 10m));
        return await CompleteTelaah(resep.ResepId, 1, 10);
    }

    private async Task<string> IntakeAndApproveTwoLines()
    {
        var resep = SeedResep("RS-2", 1, ("A", "A", 5m), ("B", "B", 6m));
        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", resep.ResepId, 0), default);
        var v1 = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, 1,
                TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 5, ""), default);
        var v2 = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", v1.TelaahResepId, v1.Version, 2,
                TelaahDispositionEnum.AcceptedAsPrescribed, "B", "B", 6, ""), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", v2.TelaahResepId, v2.Version), default);
        return resep.ResepId;
    }

    private async Task<string> CompleteTelaah(string resepId, int itemNo, decimal qty)
    {
        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", resepId, 0), default);
        var updated = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, itemNo,
                TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", qty, ""), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", updated.TelaahResepId, updated.Version), default);
        return resepId;
    }
}
