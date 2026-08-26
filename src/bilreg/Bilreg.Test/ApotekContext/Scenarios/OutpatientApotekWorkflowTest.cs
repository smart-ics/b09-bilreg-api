using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;
using Bilreg.Application.ApotekContext.JualBebasFeature.UseCases;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
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
    private readonly FakeSepPort _sep = new();
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
        var telaahId = _telaah.Store.Values.Single().TelaahResepId;
        var handler = new SalesOrderEstablishHandler(
            _so, _telaah, _resep, _jb, new FailClosedAvailableStockPort(), _copy, _tasks, _auth);
        var act = () => handler.Handle(new SalesOrderEstablishCmd("u", SalesOrderSourceKindEnum.ResepKerja, resepId,
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None, null), default);
        await act.Should().ThrowAsync<AvailableStockUnavailableException>();
        _so.Store.Should().BeEmpty();
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
        _inv.Store.Values.Single().InvoiceStatus.Should().Be(InvoiceStatusEnum.Issued);
        _inv.Store.Values.Single().GrandTotal.Should().BeGreaterThan(0);
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
    public async Task No_show_expires_dispensing_without_bpjs_invoice()
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
    }

    private async Task<string> IntakeAndApprove()
    {
        _rx.Contract = Contract();
        var intake = await new ResepKerjaIntakeElectronicHandler(_resep, _rx, _auth)
            .Handle(new ResepKerjaIntakeElectronicCmd("u", ResepKerjaSourceKindEnum.LegacyResep, "RS-1"), default);
        var start = await new TelaahStartHandler(_telaah, _resep, _auth)
            .Handle(new TelaahStartCmd("u", intake.ResepKerjaId, 0), default);
        var updated = await new TelaahUpdateItemHandler(_telaah, _auth)
            .Handle(new TelaahUpdateItemCmd("u", start.TelaahResepId, start.Version, 1, TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, ""), default);
        await new TelaahCompleteHandler(_telaah, _resep, _auth)
            .Handle(new TelaahCompleteCmd("u", updated.TelaahResepId, updated.Version), default);
        return intake.ResepKerjaId;
    }

    private async Task<SalesOrderEstablishResponse> EstablishSo(string sourceId, PayerPathEnum payer)
        => await new SalesOrderEstablishHandler(_so, _telaah, _resep, _jb, DeterministicAvailableStockPort.Full(), _copy, _tasks, _auth)
            .Handle(new SalesOrderEstablishCmd("u", SalesOrderSourceKindEnum.ResepKerja, sourceId, payer, PartialReasonEnum.None, null), default);

    private async Task PayInvoice(string salesOrderId)
    {
        var inv = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", salesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        inv = await new InvoiceIssueHandler(_inv, _tasks, _auth).Handle(new InvoiceIssueCmd("u", inv.InvoiceId, inv.Version), default);
        _pay.Evidence = new PaymentClearanceEvidence("PAY1234567890123456789012", DateTime.Now);
        await new InvoiceRecordPaymentHandler(_inv, _pay, _auth)
            .Handle(new InvoiceRecordPaymentCmd("u", inv.InvoiceId, _inv.Store.Values.Single().Version, "PAY1234567890123456789012", DateTime.Now), default);
    }

    private async Task<DispensingResponse> PrepareReady(string salesOrderId)
    {
        var d = await new DispensingEstablishHandler(_disp, _so, _auth)
            .Handle(new DispensingEstablishCmd("u", salesOrderId, [new DispensingEstablishItem(1, 10)]), default);
        d = await new DispensingReleaseHandler(_disp, _so, _inv, _policy, _auth)
            .Handle(new DispensingReleaseCmd("u", d.DispensingId, d.Version), default);
        d = await new DispensingStartHandler(_disp, _so, _inv, _tasks, _policy, _auth)
            .Handle(new DispensingStartCmd("u", d.DispensingId, d.Version, "Q1", 1, "TRK1"), default);
        return await new DispensingPrepareHandler(_disp, _auth)
            .Handle(new DispensingPrepareCmd("u", d.DispensingId, d.Version), default);
    }

    private static PrescriptionContract Contract()
        => new(ResepKerjaSourceKindEnum.LegacyResep, "RS-1", "R1", "P1", "Pasien", "D1", "Dokter", "LY01", 0, 1,
            [new PrescriptionContractItem(1, "A", "A", "TAB", "TAB", 10, 0, "3x1", "", "", false)], []);
}
