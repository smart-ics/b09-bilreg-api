using System.Text.Json;
using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature.Handlers;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ApotekContext.DispensingFeature;

public class DispensingCommandTest
{
    private readonly InMemoryDispensingRepo _disp = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly InMemoryInvoiceRepo _inv = new();
    private readonly InMemoryQueueMappingRepo _map = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
    private readonly FakePaymentPort _pay = new();
    private readonly FakePricePort _price = new();
    private readonly AllowAllAuth _auth = new();
    private readonly DispenseAuthorizedPolicy _policy = new();

    [Fact]
    public async Task Release_denied_when_general_invoice_is_issued_but_unpaid()
    {
        var order = SeedGeneralOrder();
        var invoice = await EstablishAndIssue(order.SalesOrderId);
        var dispensing = await EstablishDispensing(order.SalesOrderId);

        var act = () => new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", dispensing.DispensingId, dispensing.Version), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Dispense Authorized policy denied release*");
        _disp.Store[dispensing.DispensingId].DispensingStatus.Should().Be(DispensingStatusEnum.AwaitingClearance);
    }

    [Fact]
    public async Task Release_and_start_reevaluate_policy_after_payment_recorded()
    {
        var order = SeedGeneralOrder();
        var invoice = await EstablishAndIssue(order.SalesOrderId);
        var dispensing = await EstablishDispensing(order.SalesOrderId);

        _pay.Evidence = new PaymentClearanceEvidence("PAY1234567890123456789012", DateTime.Now);
        await new InvoiceRecordPaymentHandler(_inv, _pay, _auth)
            .Handle(new InvoiceRecordPaymentCmd("u", invoice.InvoiceId, invoice.Version, "PAY1234567890123456789012", DateTime.Now), default);

        var released = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", dispensing.DispensingId, dispensing.Version), default);
        released.Status.Should().Be(DispensingStatusEnum.Released);

        var started = await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", released.DispensingId, released.Version, "Q1", 1, "TRK1"), default);
        started.Status.Should().Be(DispensingStatusEnum.Preparing);
    }

    [Fact]
    public async Task Establish_rejects_qty_above_unresolved_accepted()
    {
        var order = SeedGeneralOrder();
        var act = () => new DispensingEstablishHandler(_disp, _so, _auth)
            .Handle(new DispensingEstablishCmd("u", order.SalesOrderId, [new DispensingEstablishItem(1, 11)]), default);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*exceeds unresolved Accepted Qty*");
        _disp.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task State_progresses_through_named_behaviors_to_prepared()
    {
        var order = SeedGeneralOrder();
        var invoice = await EstablishAndIssue(order.SalesOrderId);
        _pay.Evidence = new PaymentClearanceEvidence("PAY1234567890123456789012", DateTime.Now);
        await new InvoiceRecordPaymentHandler(_inv, _pay, _auth)
            .Handle(new InvoiceRecordPaymentCmd("u", invoice.InvoiceId, invoice.Version, "PAY1234567890123456789012", DateTime.Now), default);

        var established = await EstablishDispensing(order.SalesOrderId);
        established.Status.Should().Be(DispensingStatusEnum.AwaitingClearance);
        _disp.Store[established.DispensingId].SalesOrderId.Should().Be(order.SalesOrderId);
        _disp.Store[established.DispensingId].Items.Should().ContainSingle()
            .Which.SalesOrderItemNo.Should().Be(1);

        var released = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", established.DispensingId, established.Version), default);
        released.Status.Should().Be(DispensingStatusEnum.Released);

        var started = await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", released.DispensingId, released.Version, "Q1", 1, "TRK1"), default);
        started.Status.Should().Be(DispensingStatusEnum.Preparing);

        var prepared = await new DispensingPrepareHandler(_disp, _auth)
            .Handle(new DispensingPrepareCmd("u", started.DispensingId, started.Version), default);
        prepared.Status.Should().Be(DispensingStatusEnum.Prepared);
        _disp.Store[prepared.DispensingId].PreparedAt.Should().NotBe(ApotekDate.Empty);
    }

    [Fact]
    public async Task Start_first_enqueue_stock_reserve_and_tracker_served_at_not_at_prepare()
    {
        var order = await SeedAuthorizedBpjsOrder();
        var dispensing = await EstablishDispensing(order.SalesOrderId, 10);
        var released = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", dispensing.DispensingId, dispensing.Version), default);

        await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", released.DispensingId, released.Version, "Q1", 1, "TRK1"), default);

        _tasks.Store.Values.Should().Contain(x =>
            x.TaskType == AptIntegrationTaskTypeEnum.StockReserve
            && x.IdempotencyKey == $"{released.DispensingId}:I1:RESERVE");
        var served = _tasks.Store.Values.Single(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerServedAt);
        served.IdempotencyKey.Should().Be("Q1:1:SERVE");
        JsonDocument.Parse(served.PayloadJson).RootElement.GetProperty("ReffId").GetString()
            .Should().Be(released.DispensingId);

        await new DispensingPrepareHandler(_disp, _auth)
            .Handle(new DispensingPrepareCmd("u", released.DispensingId, _disp.Store[released.DispensingId].Version), default);

        _tasks.Store.Values.Should().NotContain(x => x.TaskType == AptIntegrationTaskTypeEnum.StockRemoveOnHandover);
        _tasks.Store.Values.Count(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerServedAt).Should().Be(1);
    }

    [Fact]
    public async Task Start_repeat_is_idempotent_without_duplicate_tasks()
    {
        var order = await SeedAuthorizedBpjsOrder();
        var dispensing = await EstablishDispensing(order.SalesOrderId, 10);
        var released = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", dispensing.DispensingId, dispensing.Version), default);
        var startHandler = new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth);
        var cmd = new DispensingStartCmd("u", released.DispensingId, released.Version, "Q1", 1, "TRK1");

        await startHandler.Handle(cmd, default);
        var versionAfterFirst = _disp.Store[released.DispensingId].Version;
        var taskCountAfterFirst = _tasks.Store.Count;

        await startHandler.Handle(new DispensingStartCmd("u", released.DispensingId, versionAfterFirst, "Q1", 1, "TRK1"), default);

        _tasks.Store.Count.Should().Be(taskCountAfterFirst);
        _disp.Store[released.DispensingId].DispensingStatus.Should().Be(DispensingStatusEnum.Preparing);
    }

    [Fact]
    public async Task Pickup_call_blocks_when_any_intended_dispensing_not_prepared()
    {
        var order = await SeedAuthorizedBpjsOrder();
        MapJualBebas("ADQ2");
        var dispensing = await EstablishDispensing(order.SalesOrderId, 10);
        var released = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", dispensing.DispensingId, dispensing.Version), default);
        await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", released.DispensingId, released.Version, "Q1", 1, "TRK1"), default);

        var act = () => new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Prepared or accountably resolved*");
    }

    [Fact]
    public async Task Pickup_call_records_pickup_called_at_and_enqueues_tracker_done()
    {
        var order = await SeedAuthorizedBpjsOrder();
        MapJualBebas("ADQ2");
        var prepared = await PrepareDispensing(order.SalesOrderId);

        var result = await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);

        result.PreparedCount.Should().Be(1);
        result.ResolvedCount.Should().Be(0);
        _disp.Store[prepared.DispensingId].PickupCalledAt.Should().NotBe(ApotekDate.Empty);
        _tasks.Store.Values.Should().ContainSingle(x =>
            x.TaskType == AptIntegrationTaskTypeEnum.TrackerDoneAtPickup
            && x.IdempotencyKey == "Q1:1:DONE");
    }

    [Fact]
    public async Task Pickup_call_succeeds_when_prepared_and_accountably_resolved_coexist()
    {
        var order1 = await SeedAuthorizedBpjsOrder();
        MapJualBebas("ADQ2");

        var order2 = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ3", "", "R3", "P3", "Pasien",
            PayerPathEnum.Bpjs, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG2", "Obat2", "TAB", 5, FornasCoverageEnum.Covered, "", false)],
            []);
        _so.SaveChanges(order2);
        await new SalesOrderApplyCoverageHandler(_so, new FakeSepPort(), _auth)
            .Handle(new SalesOrderApplyCoverageCmd("u", order2.SalesOrderId, order2.Version, "SEP-2",
                [new SalesOrderCoverageItem(1, FornasCoverageEnum.Covered)]), default);
        MapJualBebas("ADQ3");

        var prepared = await PrepareDispensing(order1.SalesOrderId);
        var expired = await PrepareDispensing(order2.SalesOrderId, order2.Items[0].AcceptedQty);
        await new DispensingNoShowHandler(_disp, _so, _tasks, _auth)
            .Handle(new DispensingNoShowCmd("u", expired.DispensingId, _disp.Store[expired.DispensingId].Version, "Q1", 1, "TRK1", "gone"), default);

        var result = await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);

        result.PreparedCount.Should().Be(1);
        result.ResolvedCount.Should().Be(1);
        _disp.Store[prepared.DispensingId].PickupCalledAt.Should().NotBe(ApotekDate.Empty);
    }

    [Fact]
    public async Task Handover_requires_education_before_completion()
    {
        var order = await SeedAuthorizedBpjsOrder();
        MapJualBebas("ADQ2");
        var prepared = await PrepareDispensing(order.SalesOrderId);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);
        var reviewed = await new DispensingFinalReviewHandler(_disp, _auth)
            .Handle(new DispensingFinalReviewCmd("u", prepared.DispensingId, _disp.Store[prepared.DispensingId].Version, FinalReviewOutcomeEnum.Pass, ""), default);
        var educated = await new DispensingEducationHandler(_disp, _auth)
            .Handle(new DispensingEducationCmd("u", reviewed.DispensingId, reviewed.Version, ""), default);

        var handed = await new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
            .Handle(new DispensingHandoverCmd("u", educated.DispensingId, educated.Version, "0812", "anak"), default);

        handed.Status.Should().Be(DispensingStatusEnum.Completed);
        _disp.Store[handed.DispensingId].RecipientPhone.Should().Be("0812");
        _disp.Store[handed.DispensingId].RecipientRelationship.Should().Be("anak");
        _tasks.Store.Values.Should().Contain(x =>
            x.TaskType == AptIntegrationTaskTypeEnum.StockRemoveOnHandover
            && x.IdempotencyKey == $"{handed.DispensingId}:I1:ISSUE");
        _inv.Store.Should().ContainSingle();
        var invoice = _inv.Store.Values.Single();
        invoice.PayerPath.Should().Be(PayerPathEnum.Bpjs);
        invoice.HasPaymentClearance.Should().BeFalse();
        _tasks.Store.Values.Should().Contain(x =>
            x.TaskType == AptIntegrationTaskTypeEnum.BillingCharge
            && x.IdempotencyKey == $"{invoice.InvoiceId}:BILL");
    }

    [Fact]
    public async Task No_show_defers_bpjs_sales_order_resolution_until_stock_return_succeeds()
    {
        var order = await SeedAuthorizedBpjsOrder();
        var prepared = await PrepareDispensing(order.SalesOrderId);
        await new DispensingNoShowHandler(_disp, _so, _tasks, _auth)
            .Handle(new DispensingNoShowCmd("u", prepared.DispensingId, _disp.Store[prepared.DispensingId].Version, "Q1", 1, "TRK1", "gone"), default);

        _so.Store[order.SalesOrderId].SalesOrderStatus.Should().NotBe(SalesOrderStatusEnum.Resolved);
        var returnTask = _tasks.Store.Values.Single(x => x.TaskType == AptIntegrationTaskTypeEnum.StockReturnNoShow);
        var stock = new Mock<IStockPharmacyPort>();
        stock.Setup(x => x.ReturnOnNoShow(prepared.DispensingId, 1, "BRG1", 10m)).Returns("MUT-RET-1");
        new StockReturnNoShowHandler(stock.Object, _disp, _so, _inv)
            .Handle(returnTask).Success.Should().BeTrue();

        _so.Store[order.SalesOrderId].SalesOrderStatus.Should().Be(SalesOrderStatusEnum.Resolved);
        _so.Store[order.SalesOrderId].ResolvedReason.Should().Be(SalesOrderResolvedReasonEnum.CollectionWindowExpired);
        _disp.Store[prepared.DispensingId].Items.Single().ReturnMutasiReff.Should().Be("MUT-RET-1");
    }

    [Fact]
    public async Task No_show_keeps_paid_general_sales_order_active_after_stock_return()
    {
        var order = SeedGeneralOrder();
        var invoice = await EstablishAndIssue(order.SalesOrderId);
        _pay.Evidence = new PaymentClearanceEvidence("PAY1234567890123456789012", DateTime.Now);
        await new InvoiceRecordPaymentHandler(_inv, _pay, _auth)
            .Handle(new InvoiceRecordPaymentCmd("u", invoice.InvoiceId, invoice.Version, "PAY1234567890123456789012", DateTime.Now), default);

        var prepared = await PrepareDispensing(order.SalesOrderId);
        await new DispensingNoShowHandler(_disp, _so, _tasks, _auth)
            .Handle(new DispensingNoShowCmd("u", prepared.DispensingId, _disp.Store[prepared.DispensingId].Version, "Q1", 1, "TRK1", "gone"), default);

        var returnTask = _tasks.Store.Values.Single(x => x.TaskType == AptIntegrationTaskTypeEnum.StockReturnNoShow);
        var stock = new Mock<IStockPharmacyPort>();
        stock.Setup(x => x.ReturnOnNoShow(prepared.DispensingId, 1, "BRG1", 10m)).Returns("MUT-RET-2");
        new StockReturnNoShowHandler(stock.Object, _disp, _so, _inv)
            .Handle(returnTask).Success.Should().BeTrue();

        _so.Store[order.SalesOrderId].SalesOrderStatus.Should().Be(SalesOrderStatusEnum.Established);
        _inv.Store.Values.Single().HasPaymentClearance.Should().BeTrue();
    }

    [Fact]
    public async Task Stock_return_failure_leaves_sales_order_active()
    {
        var order = await SeedAuthorizedBpjsOrder();
        var prepared = await PrepareDispensing(order.SalesOrderId);
        await new DispensingNoShowHandler(_disp, _so, _tasks, _auth)
            .Handle(new DispensingNoShowCmd("u", prepared.DispensingId, _disp.Store[prepared.DispensingId].Version, "Q1", 1, "TRK1", "gone"), default);

        var returnTask = _tasks.Store.Values.Single(x => x.TaskType == AptIntegrationTaskTypeEnum.StockReturnNoShow);
        var stock = new Mock<IStockPharmacyPort>();
        stock.Setup(x => x.ReturnOnNoShow(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Throws(new ApotekDomainException("inventory rejected return"));

        var result = new StockReturnNoShowHandler(stock.Object, _disp, _so, _inv).Handle(returnTask);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inventory rejected return");
        _so.Store[order.SalesOrderId].SalesOrderStatus.Should().NotBe(SalesOrderStatusEnum.Resolved);
        _disp.Store[prepared.DispensingId].Items.Single().ReturnMutasiReff.Should().BeEmpty();
    }

    private async Task<DispensingResponse> PrepareDispensing(string salesOrderId, decimal qty = 10)
    {
        var d = await EstablishDispensing(salesOrderId, qty);
        d = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", d.DispensingId, d.Version), default);
        d = await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", d.DispensingId, d.Version, "Q1", 1, "TRK1"), default);
        return await new DispensingPrepareHandler(_disp, _auth)
            .Handle(new DispensingPrepareCmd("u", d.DispensingId, d.Version), default);
    }

    private void MapJualBebas(string demandId, string antrianId = "Q1", int noUrut = 1)
    {
        _map.SaveChanges(QueueMappingModel.Create(
            QueueDemandKindEnum.JualBebas, demandId, antrianId, noUrut, "TRK1",
            QueueMappingMethodEnum.Manual, "u", DateTime.Now));
    }

    private SalesOrderModel SeedGeneralOrder()
    {
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ1", "", "R1", "P1", "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat", "TAB", 10, FornasCoverageEnum.Unknown, "", false)],
            []);
        _so.SaveChanges(order);
        return order;
    }

    private async Task<InvoiceResponse> EstablishAndIssue(string salesOrderId)
    {
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", salesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        return await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);
    }

    private SalesOrderModel SeedBpjsOrder()
    {
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ2", "", "R2", "P2", "Pasien",
            PayerPathEnum.Bpjs, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat", "TAB", 10, FornasCoverageEnum.Covered, "", false)],
            []);
        _so.SaveChanges(order);
        return order;
    }

    private async Task<SalesOrderModel> SeedAuthorizedBpjsOrder()
    {
        var order = SeedBpjsOrder();
        await new SalesOrderApplyCoverageHandler(_so, new FakeSepPort(), _auth)
            .Handle(new SalesOrderApplyCoverageCmd("u", order.SalesOrderId, order.Version, "SEP-1",
                [new SalesOrderCoverageItem(1, FornasCoverageEnum.Covered)]), default);
        return _so.Store[order.SalesOrderId];
    }

    private async Task<DispensingResponse> EstablishDispensing(string salesOrderId, decimal qty = 10)
    {
        return await new DispensingEstablishHandler(_disp, _so, _auth)
            .Handle(new DispensingEstablishCmd("u", salesOrderId, [new DispensingEstablishItem(1, qty)]), default);
    }
}
