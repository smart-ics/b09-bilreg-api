using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.SalesOrderFeature;

public class SalesOrderCommandTest
{
    private readonly InMemoryResepKerjaRepo _resep = new();
    private readonly InMemoryTelaahRepo _telaah = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly InMemoryJualBebasRepo _jb = new();
    private readonly InMemoryCopyResepRepo _copy = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
    private readonly InMemoryInvoiceRepo _inv = new();
    private readonly FakePrescriptionPort _rx = new();
    private readonly FakePricePort _price = new();
    private readonly AllowAllAuth _auth = new();

    [Fact]
    public async Task Patient_request_exclusion_issues_copy_resep_with_correct_reason_and_qty()
    {
        var resepId = await IntakeAndApproveTwoLines();
        var handler = CreateHandler(DeterministicAvailableStockPort.Full());

        var response = await handler.Handle(new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.ResepKerja, resepId,
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
        copy.Items[0].ResepKerjaItemNo.Should().Be(2);
        copy.Items[0].Qty.Should().Be(6m);
    }

    [Fact]
    public async Task Establish_rejects_patient_request_reason_when_stock_shortage_drives_exclusion()
    {
        var resepId = await IntakeAndApprove();
        var handler = CreateHandler(DeterministicAvailableStockPort.Partial(4));

        var act = () => handler.Handle(new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.ResepKerja, resepId,
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
            "u", SalesOrderSourceKindEnum.ResepKerja, resepId,
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
        _rx.Contract = ContractNoIter();
        var intake = await new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth)
            .Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-NO-ITER"), default);
        var resepId = await CompleteTelaah(intake.ResepKerjaId, 1, 10);
        var handler = CreateHandler(DeterministicAvailableStockPort.Full());

        await handler.Handle(new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.ResepKerja, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);

        _tasks.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Duplicate_establish_does_not_enqueue_second_iter_task()
    {
        var resepId = await IntakeAndApprove();
        var handler = CreateHandler(DeterministicAvailableStockPort.Full());
        var cmd = new SalesOrderEstablishCmd(
            "u", SalesOrderSourceKindEnum.ResepKerja, resepId,
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

    private async Task<string> IntakeAndApprove()
    {
        _rx.Contract = Contract();
        var intake = await new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth)
            .Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-1"), default);
        return await CompleteTelaah(intake.ResepKerjaId, 1, 10);
    }

    private async Task<string> IntakeAndApproveTwoLines()
    {
        _rx.Contract = ContractTwoLines();
        var intake = await new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth)
            .Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-2"), default);
        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", intake.ResepKerjaId, 0), default);
        var v1 = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, 1,
                TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 5, ""), default);
        var v2 = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", v1.TelaahResepId, v1.Version, 2,
                TelaahDispositionEnum.AcceptedAsPrescribed, "B", "B", 6, ""), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", v2.TelaahResepId, v2.Version), default);
        return intake.ResepKerjaId;
    }

    private async Task<string> CompleteTelaah(string resepKerjaId, int itemNo, decimal qty)
    {
        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", resepKerjaId, 0), default);
        var updated = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, itemNo,
                TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", qty, ""), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", updated.TelaahResepId, updated.Version), default);
        return resepKerjaId;
    }

    private static PrescriptionContract Contract()
        => new(ResepKerjaSourceKindEnum.LegacyResep, "RS-1", "R1", "P1", "Pasien", "D1", "Dokter", "LY01", 0, 1,
            [new PrescriptionContractItem(1, "A", "A", "TAB", "TAB", 10, 0, "3x1", "", "", false)], []);

    private static PrescriptionContract ContractNoIter()
        => new(ResepKerjaSourceKindEnum.LegacyResep, "RS-NO-ITER", "R1", "P1", "Pasien", "D1", "Dokter", "LY01", 0, 0,
            [new PrescriptionContractItem(1, "A", "A", "TAB", "TAB", 10, 0, "3x1", "", "", false)], []);

    private static PrescriptionContract ContractTwoLines()
        => new(ResepKerjaSourceKindEnum.LegacyResep, "RS-2", "R1", "P1", "Pasien", "D1", "Dokter", "LY01", 0, 1,
            [
                new PrescriptionContractItem(1, "A", "A", "TAB", "TAB", 5, 0, "3x1", "", "", false),
                new PrescriptionContractItem(2, "B", "B", "TAB", "TAB", 6, 0, "3x1", "", "", false)
            ], []);
}
