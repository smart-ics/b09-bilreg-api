namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public interface IFinancialVerificationDomainService
{
    void Verify(
        TataRekeningModel tataRekening,
        string petugasVerif,
        DateTime verifiedAt,
        IEnumerable<MergeRequestModel> pendingMergeRequests);

    void RequireAdjustment(TataRekeningModel tataRekening);

    void ResetVerification(TataRekeningModel tataRekening);
}
