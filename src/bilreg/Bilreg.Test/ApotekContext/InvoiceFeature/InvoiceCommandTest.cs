using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.InvoiceFeature;

public class InvoiceCommandTest
{
    private readonly InMemoryInvoiceRepo _inv = new();
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
    private readonly FakePricePort _price = new();
    private readonly FakePaymentPort _pay = new();
    private readonly AllowAllAuth _auth = new();

    [Fact]
    public async Task Establish_creates_invoice_from_sales_order_items_with_matching_payer_path()
    {
        var order = SeedGeneralSalesOrder();
        var snapshotBefore = DateTime.Now;

        var response = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);

        var stored = _inv.Store[response.InvoiceId];
        stored.SalesOrderId.Should().Be(order.SalesOrderId);
        stored.PayerPath.Should().Be(PayerPathEnum.GeneralPatientPay);
        stored.PricingSnapshotAt.Should().BeOnOrAfter(snapshotBefore);
        stored.Items.Should().ContainSingle();
        stored.Items[0].SalesOrderItemNo.Should().Be(1);
        stored.Items[0].Qty.Should().Be(5m);
        stored.Items[0].HargaSatuan.Should().Be(1000m);

        _so.Store[order.SalesOrderId].Item(1).InvoicedQty.Should().Be(5m);
    }

    [Fact]
    public async Task Establish_rejects_bpjs_before_handover()
    {
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ1", "", "R1", "P1", "Pasien",
            PayerPathEnum.Bpjs, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat", "TAB", 5, FornasCoverageEnum.Unknown, "", false)],
            []);
        _so.SaveChanges(order);

        var act = () => new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "BPJS", "BPJS", 0, 0, 0), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*established at handover*");
        _inv.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Fail_closed_price_port_blocks_establish()
    {
        var order = SeedGeneralSalesOrder();
        var act = () => new InvoiceEstablishHandler(_inv, _so, new FailClosedMedicationPricePort(), _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*price adapter is not configured*");
        _inv.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Issue_enqueues_single_billing_charge_task_with_deterministic_key()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);

        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);

        issued.Status.Should().Be(InvoiceStatusEnum.Issued);
        _tasks.Store.Should().ContainSingle();
        var task = _tasks.Store.Values.Single();
        task.TaskType.Should().Be(AptIntegrationTaskTypeEnum.BillingCharge);
        task.SourceKind.Should().Be(AptIntegrationSourceKindEnum.Invoice);
        task.SourceId.Should().Be(established.InvoiceId);
        task.IdempotencyKey.Should().Be($"{established.InvoiceId}:BILL");
        task.Destination.Should().Be(AptIntegrationDestinationEnum.TataRekening);
    }

    [Fact]
    public async Task Record_payment_requires_port_evidence_and_snapshots_cleared_at()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);
        var clearedAt = new DateTime(2026, 8, 27, 12, 0, 0);
        _pay.Evidence = new PaymentClearanceEvidence("PAY1234567890123456789012", clearedAt);

        var response = await new InvoiceRecordPaymentHandler(_inv, _pay, _auth)
            .Handle(new InvoiceRecordPaymentCmd("u", issued.InvoiceId, issued.Version, "PAY1234567890123456789012", DateTime.Now), default);

        response.Status.Should().Be(InvoiceStatusEnum.FinanciallyCleared);
        var stored = _inv.Store[issued.InvoiceId];
        stored.PaymentClearanceReff.Should().Be("PAY1234567890123456789012");
        stored.PaymentClearedAt.Should().Be(clearedAt);
        stored.HasPaymentClearance.Should().BeTrue();
    }

    [Fact]
    public async Task Record_payment_is_not_inferred_when_port_returns_null()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);

        var act = () => new InvoiceRecordPaymentHandler(_inv, new FailClosedPaymentClearancePort(), _auth)
            .Handle(new InvoiceRecordPaymentCmd("u", issued.InvoiceId, issued.Version, "PAY1234567890123456789012", DateTime.Now), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*not inferred from Invoice status*");
        _inv.Store[issued.InvoiceId].HasPaymentClearance.Should().BeFalse();
    }

    [Fact]
    public async Task Issue_is_idempotent_for_billing_charge_enqueue()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);

        await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);

        var issued = _inv.Store[established.InvoiceId];
        var act = () => new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", issued.InvoiceId, issued.Version), default);

        await act.Should().ThrowAsync<ApotekDomainException>();
        _tasks.Store.Should().HaveCount(1);
    }

    [Fact]
    public async Task Revise_allows_established_invoice_without_permission_port()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);

        var response = await new InvoiceReviseHandler(_inv, new DenyTrPermission(), _auth)
            .Handle(new InvoiceReviseCmd(
                "u", established.InvoiceId, established.Version,
                [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 3, 1000, 0, 0, 0, 3000)],
                [], 0, 0, 0), default);

        response.Version.Should().Be(established.Version + 1);
        _inv.Store[established.InvoiceId].Items[0].Qty.Should().Be(3m);
    }

    [Fact]
    public async Task Revise_denied_for_issued_invoice_when_permission_withheld_leaves_rows_unchanged()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);
        var before = _inv.Store[issued.InvoiceId];

        var act = () => new InvoiceReviseHandler(_inv, new DenyTrPermission(), _auth)
            .Handle(new InvoiceReviseCmd(
                "u", issued.InvoiceId, issued.Version,
                [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 9999, 0, 0, 0, 9999)],
                [], 0, 0, 0), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*immutable under current Tata Rekening permission*");
        _inv.Store[issued.InvoiceId].Items[0].HargaSatuan.Should().Be(before.Items[0].HargaSatuan);
        _inv.Store[issued.InvoiceId].Version.Should().Be(before.Version);
    }

    [Fact]
    public async Task Revise_allowed_for_issued_invoice_when_permission_granted()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);
        _inv.Store[issued.InvoiceId].RecordChargeCorrelation("TRC1");

        var response = await new InvoiceReviseHandler(_inv, new AllowTrPermission(), _auth)
            .Handle(new InvoiceReviseCmd(
                "u", issued.InvoiceId, issued.Version,
                [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 4, 1000, 0, 0, 0, 4000)],
                [], 0, 0, 0), default);

        response.Version.Should().Be(issued.Version + 1);
        _inv.Store[issued.InvoiceId].Items[0].Qty.Should().Be(4m);
    }

    [Fact]
    public async Task Record_correction_persists_tata_rekening_correlation()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);

        var response = await new InvoiceRecordCorrectionHandler(_inv, _auth)
            .Handle(new InvoiceRecordCorrectionCmd("u", issued.InvoiceId, issued.Version, "TRCN1234567890123456789"), default);

        response.Status.Should().Be(InvoiceStatusEnum.AdjustedOrCredited);
        _inv.Store[issued.InvoiceId].TataRekeningCorrectionReff.Should().Be("TRCN1234567890123456789");
    }

    [Fact]
    public async Task Correction_status_query_shows_manual_pending_until_correlation_arrives()
    {
        var order = SeedGeneralSalesOrder();
        var established = await new InvoiceEstablishHandler(_inv, _so, _price, _auth)
            .Handle(new InvoiceEstablishCmd("u", order.SalesOrderId, "UMUM", "Umum", 0, 0, 0), default);
        var issued = await new InvoiceIssueHandler(_inv, _tasks, _auth)
            .Handle(new InvoiceIssueCmd("u", established.InvoiceId, established.Version), default);
        _inv.Store[issued.InvoiceId].RecordChargeCorrelation("TRC1");

        var pending = await new InvoiceCorrectionStatusHandler(_inv, new DenyTrPermission())
            .Handle(new InvoiceCorrectionStatusQuery(issued.InvoiceId), default);
        pending.RequiresManualTataRekeningCorrection.Should().BeTrue();
        pending.AllowsDirectRevision.Should().BeFalse();
        pending.CorrectionDisposition.Should().Be(InvoiceCorrectionDispositionEnum.ManualTataRekeningCorrectionPending);

        var currentVersion = _inv.Store[issued.InvoiceId].Version;
        await new InvoiceRecordCorrectionHandler(_inv, _auth)
            .Handle(new InvoiceRecordCorrectionCmd("u", issued.InvoiceId, currentVersion, "TRCN1234567890123456789"), default);

        var correlated = await new InvoiceCorrectionStatusHandler(_inv, new DenyTrPermission())
            .Handle(new InvoiceCorrectionStatusQuery(issued.InvoiceId), default);
        correlated.CorrectionDisposition.Should().Be(InvoiceCorrectionDispositionEnum.Correlated);
        correlated.TataRekeningCorrectionReff.Should().Be("TRCN1234567890123456789");
    }

    private SalesOrderModel SeedGeneralSalesOrder()
    {
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ1", "", "R1", "P1", "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat", "TAB", 5, FornasCoverageEnum.Unknown, "", false)],
            []);
        _so.SaveChanges(order);
        return order;
    }
}
