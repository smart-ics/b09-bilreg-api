using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.InvoiceFeature;

public class DispenseAuthorizedPolicyTest
{
    private readonly DispenseAuthorizedPolicy _policy = new();

    [Fact]
    public void General_patient_requires_issued_invoice_and_payment_clearance()
    {
        var order = GeneralOrder();
        var issued = IssuedInvoice(order.SalesOrderId);
        _policy.IsAuthorized(order, issued).Should().BeFalse();

        issued.RecordPaymentClearance("PAY1234567890123456789012", DateTime.Now);
        _policy.IsAuthorized(order, issued).Should().BeTrue();
    }

    [Fact]
    public void General_patient_rejects_null_or_unpaid_invoice()
    {
        var order = GeneralOrder();
        _policy.IsAuthorized(order, null).Should().BeFalse();

        var issued = IssuedInvoice(order.SalesOrderId);
        _policy.IsAuthorized(order, issued).Should().BeFalse();
    }

    [Fact]
    public void Bpjs_requires_covered_items_with_sep()
    {
        var covered = BpjsOrder(covered: true, sepNo: "SEP-1");
        var missingSep = BpjsOrder(covered: true, sepNo: "");
        var uncovered = BpjsOrder(covered: false, sepNo: "SEP-1");

        _policy.IsAuthorized(covered, null).Should().BeTrue();
        _policy.IsAuthorized(missingSep, null).Should().BeFalse();
        _policy.IsAuthorized(uncovered, null).Should().BeFalse();
    }

    [Fact]
    public void Other_insurance_requires_payment_clearance_only()
    {
        var order = OtherInsuranceOrder();
        var invoice = EstablishedInvoice(order.SalesOrderId);
        _policy.IsAuthorized(order, invoice).Should().BeFalse();

        invoice.RecordPaymentClearance("PAY1234567890123456789012", DateTime.Now);
        _policy.IsAuthorized(order, invoice).Should().BeTrue();
    }

    private static SalesOrderModel GeneralOrder() =>
        SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ1", "", "R1", "P1", "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat", "TAB", 5, FornasCoverageEnum.Unknown, "", false)],
            []);

    private static SalesOrderModel BpjsOrder(bool covered, string sepNo) =>
        SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ2", "", "R1", "P1", "Pasien",
            PayerPathEnum.Bpjs, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(
                1, 1, "BRG1", "Obat", "TAB", 5,
                covered ? FornasCoverageEnum.Covered : FornasCoverageEnum.NotCovered,
                sepNo, false)],
            []);

    private static SalesOrderModel OtherInsuranceOrder() =>
        SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ3", "", "R1", "P1", "Pasien",
            PayerPathEnum.OtherInsurance, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat", "TAB", 5, FornasCoverageEnum.Unknown, "", false)],
            []);

    private static InvoiceModel EstablishedInvoice(string salesOrderId) =>
        InvoiceModel.Establish(
            salesOrderId, PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);

    private static InvoiceModel IssuedInvoice(string salesOrderId)
    {
        var invoice = EstablishedInvoice(salesOrderId);
        invoice.Issue(DateTime.Now);
        return invoice;
    }
}
