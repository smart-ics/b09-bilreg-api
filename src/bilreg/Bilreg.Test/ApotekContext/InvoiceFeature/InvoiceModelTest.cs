using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.InvoiceFeature;

public class InvoiceModelTest
{
    [Fact]
    public void Establish_requires_sales_order_items()
    {
        var act = () => InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [], [], 0, 0, 0);
        act.Should().Throw<ApotekDomainException>();
    }

    [Fact]
    public void Establish_records_payer_path_and_sales_order_reference()
    {
        var snapshot = new DateTime(2026, 8, 27, 10, 0, 0);
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, snapshot, "UMUM", "Umum", snapshot,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 2, 100, 0, 0, 0, 200)],
            [], 0, 0, 0);

        invoice.SalesOrderId.Should().Be("ASO1");
        invoice.PayerPath.Should().Be(PayerPathEnum.GeneralPatientPay);
        invoice.PricingSnapshotAt.Should().Be(snapshot);
        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.Established);
        invoice.Items.Should().ContainSingle();
        invoice.Items[0].SalesOrderItemNo.Should().Be(1);
    }

    [Fact]
    public void Pricing_snapshot_remains_immutable_after_issue()
    {
        var snapshot = new DateTime(2026, 8, 27, 10, 0, 0);
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, snapshot, "UMUM", "Umum", snapshot,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);

        invoice.Issue(DateTime.Now);

        invoice.PricingSnapshotAt.Should().Be(snapshot);
        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.Issued);
    }

    [Fact]
    public void Item_charges_roll_into_sum_biaya_while_header_holds_transaction_adjustments()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 2, 100, 0, 5, 0, 200)],
            [new InvoiceItemChargeModel(1, 1, "Packaging", 10)],
            diskonLain: 5, biayaLain: 3, pembulatan: 1);

        invoice.SubTotal.Should().Be(200m);
        invoice.SumBiaya.Should().Be(18m);
        invoice.DiskonLain.Should().Be(5m);
        invoice.BiayaLain.Should().Be(3m);
        invoice.Pembulatan.Should().Be(1m);
        invoice.GrandTotal.Should().Be(214m);
        invoice.Charges.Should().ContainSingle();
    }

    [Fact]
    public void Issue_only_from_established()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);

        invoice.Issue(DateTime.Now);
        var act = () => invoice.Issue(DateTime.Now);
        act.Should().Throw<ApotekDomainException>();
    }

    [Fact]
    public void Established_status_is_purchase_confirmation_without_separate_table()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);

        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.Established);
        invoice.IssuedAt.Should().Be(ApotekDate.Empty);
    }

    [Fact]
    public void Record_payment_clearance_snapshots_pd03_reference_and_timestamp()
    {
        var clearedAt = new DateTime(2026, 8, 27, 12, 0, 0);
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);
        invoice.Issue(DateTime.Now);

        invoice.RecordPaymentClearance("PAY1234567890123456789012", clearedAt);

        invoice.PaymentClearanceReff.Should().Be("PAY1234567890123456789012");
        invoice.PaymentClearedAt.Should().Be(clearedAt);
        invoice.HasPaymentClearance.Should().BeTrue();
        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.FinanciallyCleared);
    }

    [Fact]
    public void Record_payment_clearance_rejects_reference_wider_than_pd03()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);

        var act = () => invoice.RecordPaymentClearance("PAY1234567890123456789012345", DateTime.Now);
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*PD-03 width 26*");
    }

    [Fact]
    public void Established_invoice_can_rewrite_items_and_header_totals()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);

        invoice.RewriteContent(
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 2, 100, 0, 0, 0, 200)],
            [], diskonLain: 10, biayaLain: 0, pembulatan: 0, tataRekeningAllows: false);

        invoice.Items[0].Qty.Should().Be(2m);
        invoice.DiskonLain.Should().Be(10m);
        invoice.GrandTotal.Should().Be(190m);
        invoice.Version.Should().Be(2);
    }

    [Fact]
    public void Issued_invoice_rewrites_only_when_tata_rekening_allows()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);
        invoice.Issue(DateTime.Now);

        invoice.RewriteContent(
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 2, 100, 0, 0, 0, 200)],
            [], 0, 0, 0, tataRekeningAllows: true);
        invoice.Items[0].Qty.Should().Be(2m);

        var denied = InvoiceModel.Establish(
            "ASO2", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);
        denied.Issue(DateTime.Now);
        var act = () => denied.RewriteContent(
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 2, 100, 0, 0, 0, 200)],
            [], 0, 0, 0, tataRekeningAllows: false);
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*immutable under current Tata Rekening permission*");
        denied.Items[0].Qty.Should().Be(1m);
    }

    [Fact]
    public void Financially_cleared_invoice_remains_revisable_while_permission_allows()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);
        invoice.Issue(DateTime.Now);
        invoice.RecordPaymentClearance("PAY1234567890123456789012", DateTime.Now);

        invoice.RewriteContent(
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 2, 100, 0, 0, 0, 200)],
            [], 0, 0, 0, tataRekeningAllows: true);

        invoice.Items[0].Qty.Should().Be(2m);
        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.FinanciallyCleared);
    }

    [Fact]
    public void Record_correction_reff_sets_adjusted_or_credited_disposition()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);
        invoice.Issue(DateTime.Now);

        invoice.RecordCorrectionReff("TRCN1234567890123456789");

        invoice.TataRekeningCorrectionReff.Should().Be("TRCN1234567890123456789");
        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.AdjustedOrCredited);
        invoice.CorrectionDisposition(tataRekeningAllowsModification: false)
            .Should().Be(InvoiceCorrectionDispositionEnum.Correlated);
    }

    [Theory]
    [InlineData(InvoiceStatusEnum.Established, true, "", InvoiceCorrectionDispositionEnum.DirectRevisionAllowed)]
    [InlineData(InvoiceStatusEnum.Issued, true, "", InvoiceCorrectionDispositionEnum.DirectRevisionAllowed)]
    [InlineData(InvoiceStatusEnum.Issued, false, "", InvoiceCorrectionDispositionEnum.ManualTataRekeningCorrectionPending)]
    [InlineData(InvoiceStatusEnum.FinanciallyCleared, false, "", InvoiceCorrectionDispositionEnum.ManualTataRekeningCorrectionPending)]
    [InlineData(InvoiceStatusEnum.Issued, false, "TRCN1", InvoiceCorrectionDispositionEnum.Correlated)]
    public void Correction_disposition_reflects_permission_and_correlation(
        InvoiceStatusEnum status,
        bool allowsModification,
        string correctionReff,
        InvoiceCorrectionDispositionEnum expected)
    {
        var invoice = InvoiceModel.Rehydrate(
            "ASI1", "ASO1", PayerPathEnum.GeneralPatientPay, status, DateTime.Now, "UMUM", "Umum",
            100, 0, 0, 0, 0, 0, 100, "", ApotekDate.Empty, "TRC1", correctionReff,
            DateTime.Now, DateTime.Now, ApotekDate.Empty, 1,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            []);

        invoice.CorrectionDisposition(allowsModification).Should().Be(expected);
    }
}
