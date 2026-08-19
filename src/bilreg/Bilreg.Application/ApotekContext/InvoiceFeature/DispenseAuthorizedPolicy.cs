using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;

namespace Bilreg.Application.ApotekContext.InvoiceFeature;

public class DispenseAuthorizedPolicy
{
    public bool IsAuthorized(SalesOrderModel salesOrder, InvoiceModel? invoice)
    {
        return salesOrder.PayerPath switch
        {
            PayerPathEnum.GeneralPatientPay =>
                invoice is not null
                && invoice.InvoiceStatus is InvoiceStatusEnum.Issued or InvoiceStatusEnum.FinanciallyCleared
                && invoice.HasPaymentClearance,
            PayerPathEnum.Bpjs =>
                salesOrder.Items.All(x =>
                    x.FornasCoverage == FornasCoverageEnum.Covered
                    && !string.IsNullOrWhiteSpace(x.SepNo)),
            _ => invoice is not null && invoice.HasPaymentClearance
        };
    }
}

public record PaymentClearanceEvidence(string PaymentClearanceReff, DateTime ClearedAt);

public interface IPaymentClearancePort
{
    PaymentClearanceEvidence? Load(string paymentClearanceReff);
}

public interface ITataRekeningChargePort
{
    string IssueCharge(InvoiceModel invoice);
}

public interface ITataRekeningInvoicePermissionPort
{
    bool AllowsModification(string tataRekeningChargeId);
}

public record SepFornasEvidence(string SepNo, FornasCoverageEnum Coverage);

public interface ISepFornasPort
{
    SepFornasEvidence Evaluate(string regId, string brgId);
}

public interface IIterConsumePort
{
    string Consume(string sourceResepId, int consumeCount);
}

public record MedicationPrice(decimal HargaSatuan, decimal Tax);

public interface IMedicationPricePort
{
    MedicationPrice PriceAt(string brgId, DateTime snapshotAt);
}
