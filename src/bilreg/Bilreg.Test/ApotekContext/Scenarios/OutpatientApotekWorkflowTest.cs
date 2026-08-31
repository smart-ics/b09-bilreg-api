using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature.Handlers;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;
using Bilreg.Application.ApotekContext.JualBebasFeature.UseCases;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.Scenarios;

public class OutpatientApotekWorkflowTest
{
    private readonly InMemoryResepKerjaRepo _resep = new();
    private readonly InMemoryTelaahRepo _telaah = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly InMemoryJualBebasRepo _jb = new();
    private readonly InMemoryCopyResepRepo _copy = new();
    private readonly InMemoryInvoiceRepo _inv = new();
    private readonly InMemoryDispensingRepo _disp = new();
    private readonly InMemoryQueueMappingRepo _map = new();
    private readonly InMemoryQueueCloseRepo _close = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
    private readonly FakePrescriptionPort _rx = new();
    private readonly AllowAllAuth _auth = new();
    private readonly FakePaymentPort _pay = new();
    private readonly FakePricePort _price = new();
    private readonly ConfigurableSepPort _sep = new();
    private readonly FakeTrackerPort _tracker = new();
    private readonly DispenseAuthorizedPolicy _policy = new();

    [Fact]
    public async Task Electronic_intake_is_idempotent_on_source_key()
    {
        _rx.Contract = Contract();
        var handler = new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth);
        var first = await handler.Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-1"), default);
        var second = await handler.Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-1"), default);
        first.IdempotentReplay.Should().BeFalse();
        second.IdempotentReplay.Should().BeTrue();
        second.ResepKerjaId.Should().Be(first.ResepKerjaId);
        _resep.Store.Should().HaveCount(1);
    }

    [Fact]
    public async Task Jual_bebas_accept_creates_one_header_row()
    {
        _jb.Store.Should().BeEmpty();
        var accept = new JualBebasAcceptHandler(_jb, _auth);
        await accept.Handle(new JualBebasAcceptCmd("u", "R1", "P1", "Pasien", [
            new JualBebasAcceptItem(1, "A", "A", "TAB", 2, "3x1")
        ]), default);
        _jb.Store.Should().HaveCount(1);
    }

    [Fact]
    public async Task Available_stock_fail_closed_commits_no_sales_order()
    {
        var resepId = await IntakeAndApprove();
        var handler = new SalesOrderEstablishHandler(
            _so, _telaah, _resep, _jb, new FailClosedAvailableStockPort(), _copy, _tasks, _auth);
        var act = () => handler.Handle(new SalesOrderEstablishCmd("u", SalesOrderSourceKindEnum.ResepKerja, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);
        await act.Should().ThrowAsync<AvailableStockUnavailableException>();
        _so.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Available_stock_partial_trims_accepted_qty_and_issues_copy_resep()
    {
        var resepId = await IntakeAndApprove();
        var handler = new SalesOrderEstablishHandler(
            _so, _telaah, _resep, _jb, DeterministicAvailableStockPort.Partial(4), _copy, _tasks, _auth);

        var response = await handler.Handle(new SalesOrderEstablishCmd("u", SalesOrderSourceKindEnum.ResepKerja, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);

        var stored = _so.Store.Values.Single();
        stored.SalesOrderId.Should().Be(response.SalesOrderId);
        stored.Items.Should().ContainSingle();
        stored.Items[0].AcceptedQty.Should().Be(4m);
        stored.PartialReason.Should().Be(PartialReasonEnum.StockShortage);
        response.CopyResepId.Should().NotBeNullOrWhiteSpace();
        _copy.Store.Should().ContainSingle();
        var copy = _copy.Store.Values.Single();
        copy.CopyResepId.Should().Be(response.CopyResepId);
        copy.Items.Should().ContainSingle();
        copy.Items[0].Qty.Should().Be(6m);
    }

    [Fact]
    public async Task Available_stock_zero_commits_no_sales_order()
    {
        var resepId = await IntakeAndApprove();
        var handler = new SalesOrderEstablishHandler(
            _so, _telaah, _resep, _jb, DeterministicAvailableStockPort.Zero(), _copy, _tasks, _auth);
        var act = () => handler.Handle(new SalesOrderEstablishCmd("u", SalesOrderSourceKindEnum.ResepKerja, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("No positive Accepted Qty remains after Available Stock evaluation.");
        _so.Store.Should().BeEmpty();
        _copy.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Wf001_map_multi_demand_and_queue_close_from_waiting()
    {
        var resepId = await IntakeAndApprove("RS-WF1-A");
        var jualBebasId = await AcceptJualBebas();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.JualBebas, jualBebasId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);

        _map.ListByQueue("Q1", 1).Should().HaveCount(2);
        _tasks.Store.Values.Should().NotContain(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerServedAt);
        _tasks.Store.Values.Should().NotContain(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerDoneAtPickup);

        _tracker.Status = AntrianStatusEnum.Waiting;
        await new QueueCloseHandler(_close, _tracker, _tasks, _auth)
            .Handle(new QueueCloseCmd("u", "Q1", 1, "patient left"), default);

        _close.Store.Should().HaveCount(1);
        _tasks.Store.Values.Should().ContainSingle(x =>
            x.TaskType == AptIntegrationTaskTypeEnum.TrackerWithdrawn
            && x.IdempotencyKey.EndsWith(":WDN"));
    }

    [Fact]
    public async Task Wf002_intake_telaah_establish_sales_order_and_reject_blocked()
    {
        var resepId = await IntakeAndApprove("RS-WF2-OK");
        var so = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        so.SalesOrderId.Should().NotBeNullOrWhiteSpace();
        _so.Store[so.SalesOrderId].Items.Should().ContainSingle(x => x.AcceptedQty == 10m);

        _rx.Contract = Contract("RS-WF2-REJ");
        var intake = await new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth)
            .Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-WF2-REJ"), default);
        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", intake.ResepKerjaId, 0), default);
        var updated = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, 1,
                TelaahDispositionEnum.Rejected, "A", "A", 0, "contraindicated"), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", updated.TelaahResepId, updated.Version), default);

        var act = () => EstablishSo(intake.ResepKerjaId, PayerPathEnum.GeneralPatientPay);
        await act.Should().ThrowAsync<ApotekDomainException>();
    }

    [Fact]
    public async Task Wf003_general_patient_happy_path()
    {
        var resepId = await IntakeAndApprove();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        var so = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        var inv = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", so.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", inv.InvoiceId, inv.Version), default);
        _pay.Evidence = new PaymentClearanceEvidence("PAY1234567890123456789012", DateTime.Now);
        var issued = _inv.Store.Values.Single();
        await new InvoiceRecordPaymentHandler(_inv, _pay, _auth)
            .Handle(new InvoiceRecordPaymentCmd("u", issued.InvoiceId, issued.Version, "PAY1234567890123456789012", DateTime.Now), default);

        var d = await new DispensingEstablishHandler(_disp, _so, _auth)
            .Handle(new DispensingEstablishCmd("u", so.SalesOrderId, [new DispensingEstablishItem(1, 10)]), default);
        d = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", d.DispensingId, d.Version), default);
        d = await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", d.DispensingId, d.Version, "Q1", 1, "TRK1"), default);
        d = await new DispensingPrepareHandler(_disp, _auth)
            .Handle(new DispensingPrepareCmd("u", d.DispensingId, d.Version), default);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);
        d = await new DispensingFinalReviewHandler(_disp, _auth)
            .Handle(new DispensingFinalReviewCmd("u", d.DispensingId, _disp.Store.Values.Single().Version, FinalReviewOutcomeEnum.Pass, ""), default);
        d = await new DispensingEducationHandler(_disp, _auth)
            .Handle(new DispensingEducationCmd("u", d.DispensingId, d.Version, "ok"), default);
        d = await new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
            .Handle(new DispensingHandoverCmd("u", d.DispensingId, d.Version, "0812", "anak"), default);
        d.Status.Should().Be(DispensingStatusEnum.Completed);
        _tasks.Store.Values.Should().Contain(x => x.TaskType == Domain.ApotekContext.IntegrationFeature.AptIntegrationTaskTypeEnum.TrackerServedAt);
        _tasks.Store.Values.Should().Contain(x => x.TaskType == Domain.ApotekContext.IntegrationFeature.AptIntegrationTaskTypeEnum.TrackerDoneAtPickup);
        _tasks.Store.Values.Should().Contain(x => x.TaskType == Domain.ApotekContext.IntegrationFeature.AptIntegrationTaskTypeEnum.StockRemoveOnHandover);
        _tasks.Store.Values.Should().NotContain(x => x.PayloadJson.Contains("tb_trs_dobill_umum"));
    }

    [Fact]
    public async Task Wf003_decline_before_invoice_creates_no_invoice_and_resolves_demand()
    {
        var resepId = await IntakeAndApprove();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        var so = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);

        await new SalesOrderDeclinePurchaseHandler(_so, _inv, _disp, _tasks, _auth)
            .Handle(new SalesOrderDeclinePurchaseCmd("u", so.SalesOrderId, so.Version), default);

        _inv.Store.Should().BeEmpty();
        var stored = _so.Store.Values.Single();
        stored.SalesOrderStatus.Should().Be(SalesOrderStatusEnum.Resolved);
        stored.ResolvedReason.Should().Be(SalesOrderResolvedReasonEnum.PatientDeclined);
        stored.Outcomes.Should().ContainSingle(x => x.Reason == UnfulfilledReasonEnum.PatientDecline && x.Qty == 10m);
    }

    [Fact]
    public async Task Wf003_payment_incomplete_blocks_preparation_without_corrupting_prior_facts()
    {
        var resepId = await IntakeAndApprove();
        var so = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        var inv = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", so.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        inv = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", inv.InvoiceId, inv.Version), default);
        var issuedVersion = inv.Version;
        var d = await new DispensingEstablishHandler(_disp, _so, _auth)
            .Handle(new DispensingEstablishCmd("u", so.SalesOrderId, [new DispensingEstablishItem(1, 10)]), default);

        var release = () => new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", d.DispensingId, d.Version), default);
        await release.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Dispense Authorized policy denied release*");

        _inv.Store.Values.Single().HasPaymentClearance.Should().BeFalse();
        _inv.Store.Values.Single().Version.Should().Be(issuedVersion);
        _disp.Store[d.DispensingId].DispensingStatus.Should().Be(DispensingStatusEnum.AwaitingClearance);
    }

    [Fact]
    public async Task Wf003_tracker_done_is_not_handover_proof()
    {
        var resepId = await IntakeAndApprove();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        var so = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        await PayInvoice(so.SalesOrderId);
        var d = await PrepareReady(so.SalesOrderId);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);

        _tasks.Store.Values.Should().Contain(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerDoneAtPickup);
        _disp.Store.Values.Single().DispensingStatus.Should().Be(DispensingStatusEnum.Prepared);
        _disp.Store.Values.Single().HandoverAt.Should().Be(ApotekDate.Empty);

        var handover = () => new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
            .Handle(new DispensingHandoverCmd("u", d.DispensingId, _disp.Store.Values.Single().Version, "", ""), default);
        await handover.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Handover requires patient education acknowledgement*");
    }

    [Fact]
    public async Task Wf003_cross_context_billing_failure_leaves_retryable_task_and_preserved_invoice()
    {
        var resepId = await IntakeAndApprove();
        var so = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        var inv = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", so.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        inv = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", inv.InvoiceId, inv.Version), default);
        var billingTask = _tasks.Store.Values.Single(x => x.TaskType == AptIntegrationTaskTypeEnum.BillingCharge);

        var worker = new AptIntegrationWorker(_tasks, [new BillingChargeHandler(new FailClosedTataRekeningChargePort(), _inv)]);
        var result = worker.ProcessOne(billingTask.IntegrationTaskId);

        result.Success.Should().BeFalse();
        _tasks.Store[billingTask.IntegrationTaskId].TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Failed);
        _tasks.Store[billingTask.IntegrationTaskId].RetryCount.Should().Be(1);
        _inv.Store.Values.Single().InvoiceStatus.Should().Be(InvoiceStatusEnum.Issued);
        string.IsNullOrWhiteSpace(_inv.Store.Values.Single().TataRekeningChargeId).Should().BeTrue();
    }

    [Fact]
    public void Wf003_tracker_adapter_contract_maps_served_and_done_payloads()
    {
        var tracker = new FakeTrackerPort();
        var servedTask = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP000000001",
            "Q1:1:SERVE",
            AptIntegrationDestinationEnum.Tracker,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                AntrianId = "Q1",
                NoUrut = 1,
                PasienTrackerId = "TRK1",
                ReffId = "ADP000000001"
            }));
        new TrackerServedAtHandler(tracker).Handle(servedTask).Success.Should().BeTrue();
        tracker.ServeCount.Should().Be(1);

        var doneTask = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerDoneAtPickup,
            AptIntegrationSourceKindEnum.Mapping,
            "Q1:1",
            "Q1:1:DONE",
            AptIntegrationDestinationEnum.Tracker,
            System.Text.Json.JsonSerializer.Serialize(new { AntrianId = "Q1", NoUrut = 1, PasienTrackerId = "TRK1" }));
        new TrackerDoneAtPickupHandler(tracker).Handle(doneTask).Success.Should().BeTrue();
        tracker.DoneCount.Should().Be(1);
        tracker.Status.Should().Be(AntrianStatusEnum.Done);
    }

    [Fact]
    public async Task Wf004_bpjs_invoice_only_after_handover()
    {
        var resepId = await IntakeAndApprove();
        var so = await EstablishSo(resepId, PayerPathEnum.Bpjs);
        await new SalesOrderApplyCoverageHandler(_so, _sep, _auth)
            .Handle(new SalesOrderApplyCoverageCmd("u", so.SalesOrderId, so.Version, "SEP-1", [new SalesOrderCoverageItem(1, FornasCoverageEnum.Covered)]), default);
        var early = () => new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", so.SalesOrderId, "BPJS", "BPJS", 0, 0, 0), default);
        await early.Should().ThrowAsync<ApotekDomainException>();

        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        _inv.Store.Should().BeEmpty();
        _policy.IsAuthorized(_so.Store[so.SalesOrderId], null).Should().BeTrue();
        var d = await PrepareReady(so.SalesOrderId);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);
        await new DispensingFinalReviewHandler(_disp, _auth)
            .Handle(new DispensingFinalReviewCmd("u", d.DispensingId, _disp.Store.Values.Single().Version, FinalReviewOutcomeEnum.Pass, ""), default);
        await new DispensingEducationHandler(_disp, _auth)
            .Handle(new DispensingEducationCmd("u", d.DispensingId, _disp.Store.Values.Single().Version, ""), default);
        await new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
            .Handle(new DispensingHandoverCmd("u", d.DispensingId, _disp.Store.Values.Single().Version, "", ""), default);
        _inv.Store.Should().HaveCount(1);
        var invoice = _inv.Store.Values.Single();
        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.Issued);
        invoice.PayerPath.Should().Be(PayerPathEnum.Bpjs);
        invoice.GrandTotal.Should().BeGreaterThan(0);
        invoice.HasPaymentClearance.Should().BeFalse();
        _tasks.Store.Values.Should().ContainSingle(x =>
            x.TaskType == AptIntegrationTaskTypeEnum.BillingCharge
            && x.SourceId == invoice.InvoiceId
            && x.IdempotencyKey == $"{invoice.InvoiceId}:BILL");
    }

    [Fact]
    public async Task Wf004_failed_final_review_blocks_handover_without_bpjs_invoice()
    {
        var resepId = await IntakeAndApprove();
        var so = await EstablishSo(resepId, PayerPathEnum.Bpjs);
        await new SalesOrderApplyCoverageHandler(_so, _sep, _auth)
            .Handle(new SalesOrderApplyCoverageCmd("u", so.SalesOrderId, so.Version, "SEP-1", [new SalesOrderCoverageItem(1, FornasCoverageEnum.Covered)]), default);
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        var d = await PrepareReady(so.SalesOrderId);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);
        var after = await new DispensingFinalReviewHandler(_disp, _auth)
            .Handle(new DispensingFinalReviewCmd("u", d.DispensingId, _disp.Store.Values.Single().Version, FinalReviewOutcomeEnum.Fail, "wrong"), default);
        after.Status.Should().Be(DispensingStatusEnum.Preparing);
        var handover = () => new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
            .Handle(new DispensingHandoverCmd("u", d.DispensingId, after.Version, "", ""), default);
        await handover.Should().ThrowAsync<ApotekDomainException>();
        _inv.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Queue_close_rejects_in_service()
    {
        _tracker.Status = AntrianStatusEnum.InService;
        var handler = new QueueCloseHandler(_close, _tracker, _tasks, _auth);
        var act = () => handler.Handle(new QueueCloseCmd("u", "Q1", 1, "no show"), default);
        await act.Should().ThrowAsync<InvalidOperationException>();
        _close.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Failed_final_review_blocks_handover_and_returns_preparing()
    {
        var resepId = await IntakeAndApprove();
        var so = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        await PayInvoice(so.SalesOrderId);
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        var d = await PrepareReady(so.SalesOrderId);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);
        var after = await new DispensingFinalReviewHandler(_disp, _auth)
            .Handle(new DispensingFinalReviewCmd("u", d.DispensingId, _disp.Store.Values.Single().Version, FinalReviewOutcomeEnum.Fail, "wrong"), default);
        after.Status.Should().Be(DispensingStatusEnum.Preparing);
        var handover = () => new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
            .Handle(new DispensingHandoverCmd("u", d.DispensingId, after.Version, "", ""), default);
        await handover.Should().ThrowAsync<ApotekDomainException>();
    }

    [Fact]
    public async Task Wf007_bpjs_no_show_expires_dispensing_without_invoice()
    {
        var resepId = await IntakeAndApprove();
        var so = await EstablishSo(resepId, PayerPathEnum.Bpjs);
        await new SalesOrderApplyCoverageHandler(_so, _sep, _auth)
            .Handle(new SalesOrderApplyCoverageCmd("u", so.SalesOrderId, so.Version, "SEP-1", [new SalesOrderCoverageItem(1, FornasCoverageEnum.Covered)]), default);
        var d = await PrepareReady(so.SalesOrderId);
        await new DispensingNoShowHandler(_disp, _so, _tasks, _auth)
            .Handle(new DispensingNoShowCmd("u", d.DispensingId, _disp.Store.Values.Single().Version, "Q1", 1, "TRK1", "uncollected"), default);
        _disp.Store.Values.Single().DispensingStatus.Should().Be(DispensingStatusEnum.Expired);
        _inv.Store.Should().BeEmpty();
        _so.Store.Values.Single().Outcomes.Should().NotBeEmpty();
        _so.Store.Values.Single().SalesOrderStatus.Should().NotBe(SalesOrderStatusEnum.Resolved);
    }

    [Fact]
    public async Task Wf005_mixed_coverage_happy_path()
    {
        var resepId = await IntakeMixedAndApprove();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);

        var mixed = await new SalesOrderEstablishMixedHandler(
                _so, _telaah, _resep, DeterministicAvailableStockPort.Full(), _tasks, _sep, _auth)
            .Handle(new SalesOrderEstablishMixedCmd("u", resepId, "SEP-MIX"), default);

        var bpjs = _so.Store[mixed.BpjsSalesOrderId!];
        var patientPay = _so.Store[mixed.PatientPaySalesOrderId!];
        bpjs.Items.Should().ContainSingle(x => x.SourceItemNo == 1);
        patientPay.Items.Should().ContainSingle(x => x.SourceItemNo == 2);

        await PayInvoice(patientPay.SalesOrderId);
        _inv.Store.Values.Should().ContainSingle(x => x.PayerPath == PayerPathEnum.GeneralPatientPay);

        var bpjsDisp = await PrepareReady(bpjs.SalesOrderId, 5);
        var patientDisp = await PrepareReady(patientPay.SalesOrderId, 6);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);

        foreach (var dispensingId in new[] { bpjsDisp.DispensingId, patientDisp.DispensingId })
        {
            var version = _disp.Store[dispensingId].Version;
            await new DispensingFinalReviewHandler(_disp, _auth)
                .Handle(new DispensingFinalReviewCmd("u", dispensingId, version, FinalReviewOutcomeEnum.Pass, ""), default);
            version = _disp.Store[dispensingId].Version;
            await new DispensingEducationHandler(_disp, _auth)
                .Handle(new DispensingEducationCmd("u", dispensingId, version, ""), default);
            version = _disp.Store[dispensingId].Version;
            await new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
                .Handle(new DispensingHandoverCmd("u", dispensingId, version, "", ""), default);
        }

        _inv.Store.Values.Should().Contain(x => x.PayerPath == PayerPathEnum.Bpjs);
        _inv.Store.Values.Should().Contain(x => x.PayerPath == PayerPathEnum.GeneralPatientPay);
        _disp.Store.Values.Should().OnlyContain(x => x.DispensingStatus == DispensingStatusEnum.Completed);
    }

    [Fact]
    public async Task Wf005_patient_decline_patient_pay_continues_bpjs_path()
    {
        var resepId = await IntakeMixedAndApprove();
        var mixed = await new SalesOrderEstablishMixedHandler(
                _so, _telaah, _resep, DeterministicAvailableStockPort.Full(), _tasks, _sep, _auth)
            .Handle(new SalesOrderEstablishMixedCmd("u", resepId, "SEP-MIX"), default);

        await new SalesOrderDeclinePurchaseHandler(_so, _inv, _disp, _tasks, _auth)
            .Handle(new SalesOrderDeclinePurchaseCmd("u", mixed.PatientPaySalesOrderId!, mixed.PatientPayVersion!.Value), default);

        _so.Store[mixed.PatientPaySalesOrderId!].SalesOrderStatus.Should().Be(SalesOrderStatusEnum.Resolved);
        _so.Store[mixed.BpjsSalesOrderId!].IsActiveKey.Should().BeTrue();
        _inv.Store.Should().BeEmpty();

        var bpjsDisp = await PrepareReady(mixed.BpjsSalesOrderId!, 5);
        bpjsDisp.Status.Should().Be(DispensingStatusEnum.Prepared);
    }

    [Fact]
    public async Task Wf006_multi_demand_happy_path_keeps_separate_lifecycles_and_one_queue_milestone()
    {
        var resepId = await IntakeAndApprove("RS-WF6-A");
        var jualBebasId = await AcceptJualBebas();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.JualBebas, jualBebasId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);

        var resepSo = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        var jualSo = await EstablishSo(jualBebasId, PayerPathEnum.GeneralPatientPay, SalesOrderSourceKindEnum.JualBebas);
        resepSo.SalesOrderId.Should().NotBe(jualSo.SalesOrderId);

        await PayInvoice(resepSo.SalesOrderId);
        await PayInvoice(jualSo.SalesOrderId);
        _inv.Store.Values.Should().HaveCount(2);

        var resepDisp = await PrepareReady(resepSo.SalesOrderId);
        var jualDisp = await PrepareReady(jualSo.SalesOrderId, 2);
        _tasks.Store.Values.Count(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerServedAt).Should().Be(1);
        _tasks.Store.Values.Single(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerServedAt)
            .IdempotencyKey.Should().Be("Q1:1:SERVE");

        var pickup = await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);
        pickup.PreparedCount.Should().Be(2);
        _tasks.Store.Values.Count(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerDoneAtPickup).Should().Be(1);
        _tasks.Store.Values.Single(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerDoneAtPickup)
            .IdempotencyKey.Should().Be("Q1:1:DONE");

        foreach (var dispensingId in new[] { resepDisp.DispensingId, jualDisp.DispensingId })
        {
            var version = _disp.Store[dispensingId].Version;
            await new DispensingFinalReviewHandler(_disp, _auth)
                .Handle(new DispensingFinalReviewCmd("u", dispensingId, version, FinalReviewOutcomeEnum.Pass, ""), default);
            version = _disp.Store[dispensingId].Version;
            await new DispensingEducationHandler(_disp, _auth)
                .Handle(new DispensingEducationCmd("u", dispensingId, version, ""), default);
            version = _disp.Store[dispensingId].Version;
            await new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
                .Handle(new DispensingHandoverCmd("u", dispensingId, version, "", ""), default);
        }

        _disp.Store.Values.Should().HaveCount(2);
        _disp.Store.Values.Should().OnlyContain(x => x.DispensingStatus == DispensingStatusEnum.Completed);
        _so.Store.Values.Should().HaveCount(2);
        _so.Store.Values.SelectMany(x => x.Items).Should().OnlyContain(x => x.DispensedQty > 0);
    }

    [Fact]
    public async Task Wf006_two_preparation_starts_enqueue_only_one_tracker_served_at()
    {
        var resepId = await IntakeAndApprove("RS-WF6-B");
        var jualBebasId = await AcceptJualBebas();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.JualBebas, jualBebasId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);

        var resepSo = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        var jualSo = await EstablishSo(jualBebasId, PayerPathEnum.GeneralPatientPay, SalesOrderSourceKindEnum.JualBebas);
        await PayInvoice(resepSo.SalesOrderId);
        await PayInvoice(jualSo.SalesOrderId);

        var resepDisp = await EstablishAndRelease(resepSo.SalesOrderId);
        var jualDisp = await EstablishAndRelease(jualSo.SalesOrderId, 2);

        await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", resepDisp.DispensingId, resepDisp.Version, "Q1", 1, "TRK1"), default);
        _tasks.Store.Values.Count(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerServedAt).Should().Be(1);

        var jualVersion = _disp.Store[jualDisp.DispensingId].Version;
        await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", jualDisp.DispensingId, jualVersion, "Q1", 1, "TRK1"), default);
        _tasks.Store.Values.Count(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerServedAt).Should().Be(1);
    }

    [Fact]
    public async Task Wf006_failed_review_on_one_demand_does_not_rewrite_sibling_dispensing()
    {
        var resepId = await IntakeAndApprove("RS-WF6-C");
        var jualBebasId = await AcceptJualBebas();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.JualBebas, jualBebasId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);

        var resepSo = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        var jualSo = await EstablishSo(jualBebasId, PayerPathEnum.GeneralPatientPay, SalesOrderSourceKindEnum.JualBebas);
        await PayInvoice(resepSo.SalesOrderId);
        await PayInvoice(jualSo.SalesOrderId);

        var resepDisp = await PrepareReady(resepSo.SalesOrderId);
        var jualDisp = await PrepareReady(jualSo.SalesOrderId, 2);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);

        var failed = await new DispensingFinalReviewHandler(_disp, _auth)
            .Handle(new DispensingFinalReviewCmd("u", resepDisp.DispensingId, _disp.Store[resepDisp.DispensingId].Version,
                FinalReviewOutcomeEnum.Fail, "wrong"), default);
        failed.Status.Should().Be(DispensingStatusEnum.Preparing);
        _disp.Store[jualDisp.DispensingId].DispensingStatus.Should().Be(DispensingStatusEnum.Prepared);

        var reviewed = await new DispensingFinalReviewHandler(_disp, _auth)
            .Handle(new DispensingFinalReviewCmd("u", jualDisp.DispensingId, _disp.Store[jualDisp.DispensingId].Version,
                FinalReviewOutcomeEnum.Pass, ""), default);
        reviewed = await new DispensingEducationHandler(_disp, _auth)
            .Handle(new DispensingEducationCmd("u", reviewed.DispensingId, reviewed.Version, ""), default);
        var handed = await new DispensingHandoverHandler(_disp, _so, _inv, _price, _tasks, new FakeWindow(), _auth)
            .Handle(new DispensingHandoverCmd("u", reviewed.DispensingId, reviewed.Version, "", ""), default);
        handed.Status.Should().Be(DispensingStatusEnum.Completed);
        _disp.Store[resepDisp.DispensingId].DispensingStatus.Should().Be(DispensingStatusEnum.Preparing);
    }

    [Fact]
    public async Task Wf006_partial_pickup_rejected_when_sibling_demand_unresolved()
    {
        var resepId = await IntakeAndApprove("RS-WF6-E");
        var jualBebasId = await AcceptJualBebas();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.JualBebas, jualBebasId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);

        var resepSo = await EstablishSo(resepId, PayerPathEnum.GeneralPatientPay);
        var jualSo = await EstablishSo(jualBebasId, PayerPathEnum.GeneralPatientPay, SalesOrderSourceKindEnum.JualBebas);
        await PayInvoice(resepSo.SalesOrderId);
        await PayInvoice(jualSo.SalesOrderId);
        await PrepareReady(resepSo.SalesOrderId);
        await EstablishAndRelease(jualSo.SalesOrderId, 2);

        var act = () => new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Prepared or accountably resolved*");
        _tasks.Store.Values.Should().NotContain(x => x.TaskType == AptIntegrationTaskTypeEnum.TrackerDoneAtPickup);
    }

    [Fact]
    public async Task Wf005_failed_review_on_patient_pay_arm_does_not_rewrite_bpjs_order()
    {
        var resepId = await IntakeMixedAndApprove();
        await new QueueMapHandler(_map, _resep, _jb, _auth).Handle(
            new QueueMapCmd("u", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);
        var mixed = await new SalesOrderEstablishMixedHandler(
                _so, _telaah, _resep, DeterministicAvailableStockPort.Full(), _tasks, _sep, _auth)
            .Handle(new SalesOrderEstablishMixedCmd("u", resepId, "SEP-MIX"), default);

        await PayInvoice(mixed.PatientPaySalesOrderId!);
        var bpjsDisp = await PrepareReady(mixed.BpjsSalesOrderId!, 5);
        var patientDisp = await PrepareReady(mixed.PatientPaySalesOrderId!, 6);
        await new DispensingPickupCallHandler(_map, _so, _disp, _tasks, _auth)
            .Handle(new DispensingPickupCallCmd("u", "Q1", 1, "TRK1"), default);

        var failed = await new DispensingFinalReviewHandler(_disp, _auth)
            .Handle(new DispensingFinalReviewCmd("u", patientDisp.DispensingId, _disp.Store[patientDisp.DispensingId].Version,
                FinalReviewOutcomeEnum.Fail, "wrong"), default);
        failed.Status.Should().Be(DispensingStatusEnum.Preparing);
        _disp.Store[bpjsDisp.DispensingId].DispensingStatus.Should().Be(DispensingStatusEnum.Prepared);
        _so.Store[mixed.BpjsSalesOrderId!].IsActiveKey.Should().BeTrue();
    }

    private async Task<string> IntakeAndApprove(string sourceKey = "RS-1")
    {
        _sep.CoverageByBrgId.Clear();
        _sep.CoverageByBrgId["A"] = FornasCoverageEnum.Covered;
        _rx.Contract = Contract(sourceKey);
        var intake = await new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth)
            .Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, sourceKey), default);
        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", intake.ResepKerjaId, 0), default);
        var updated = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, 1, TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, ""), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", updated.TelaahResepId, updated.Version), default);
        return intake.ResepKerjaId;
    }

    private async Task<string> AcceptJualBebas()
    {
        var accept = new JualBebasAcceptHandler(_jb, _auth);
        var response = await accept.Handle(new JualBebasAcceptCmd("u", "R-JB", "P-JB", "Pasien JB", [
            new JualBebasAcceptItem(1, "B", "Obat B", "TAB", 2, "3x1")
        ]), default);
        return response.JualBebasId;
    }

    private async Task<string> IntakeMixedAndApprove()
    {
        _sep.CoverageByBrgId.Clear();
        _sep.CoverageByBrgId["A"] = FornasCoverageEnum.Covered;
        _sep.CoverageByBrgId["B"] = FornasCoverageEnum.NotCovered;
        _rx.Contract = ContractTwoLines();
        var intake = await new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth)
            .Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-MIX"), default);
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

    private async Task<SalesOrderEstablishResponse> EstablishSo(
        string sourceId, PayerPathEnum payer, SalesOrderSourceKindEnum sourceKind = SalesOrderSourceKindEnum.ResepKerja)
        => await new SalesOrderEstablishHandler(_so, _telaah, _resep, _jb, DeterministicAvailableStockPort.Full(), _copy, _tasks, _auth)
            .Handle(new SalesOrderEstablishCmd("u", sourceKind, sourceId, payer, PartialReasonEnum.None, null), default);

    private async Task<DispensingResponse> EstablishAndRelease(string salesOrderId, decimal qty = 10)
    {
        var d = await new DispensingEstablishHandler(_disp, _so, _auth)
            .Handle(new DispensingEstablishCmd("u", salesOrderId, [new DispensingEstablishItem(1, qty)]), default);
        return await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", d.DispensingId, d.Version), default);
    }

    private async Task PayInvoice(string salesOrderId)
    {
        var inv = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", salesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        inv = await new InvoiceIssueHandler(_inv, _tasks, _auth).Handle(new InvoiceIssueCmd("u", inv.InvoiceId, inv.Version), default);
        _pay.Evidence = new PaymentClearanceEvidence("PAY1234567890123456789012", DateTime.Now);
        var stored = _inv.Store.Values.Single(x => x.SalesOrderId == salesOrderId);
        await new InvoiceRecordPaymentHandler(_inv, _pay, _auth)
            .Handle(new InvoiceRecordPaymentCmd("u", stored.InvoiceId, stored.Version, "PAY1234567890123456789012", DateTime.Now), default);
    }

    private async Task<DispensingResponse> PrepareReady(string salesOrderId, decimal qty = 10)
    {
        var d = await new DispensingEstablishHandler(_disp, _so, _auth)
            .Handle(new DispensingEstablishCmd("u", salesOrderId, [new DispensingEstablishItem(1, qty)]), default);
        d = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", d.DispensingId, d.Version), default);
        d = await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", d.DispensingId, d.Version, "Q1", 1, "TRK1"), default);
        return await new DispensingPrepareHandler(_disp, _auth)
            .Handle(new DispensingPrepareCmd("u", d.DispensingId, d.Version), default);
    }

    private static PrescriptionContract Contract(string sourceKey = "RS-1")
        => new(ResepKerjaSourceKindEnum.LegacyResep, sourceKey, "R1", "P1", "Pasien", "D1", "Dokter", "LY01", 0, 1,
            [new PrescriptionContractItem(1, "A", "A", "TAB", "TAB", 10, 0, "3x1", "", "", false)], []);

    private static PrescriptionContract ContractTwoLines()
        => new(ResepKerjaSourceKindEnum.LegacyResep, "RS-MIX", "R1", "P1", "Pasien", "D1", "Dokter", "LY01", 0, 0,
            [
                new PrescriptionContractItem(1, "A", "A", "TAB", "TAB", 5, 0, "3x1", "", "", false),
                new PrescriptionContractItem(2, "B", "B", "TAB", "TAB", 6, 0, "3x1", "", "", false)
            ], []);
}
