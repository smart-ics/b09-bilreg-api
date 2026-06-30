namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

/// <summary>
/// Transfers Accounts Receivable ownership between registrations during Merge Billing (SOP-TR-03).
/// </summary>
public interface ITransferReceivableService
{
    void Transfer(string sourceRegId, string targetRegId);
}
