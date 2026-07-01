namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public sealed class FinancialVerificationDomainService : IFinancialVerificationDomainService
{
    public void Verify(
        TataRekeningModel tataRekening,
        string petugasVerif,
        DateTime verifiedAt,
        IEnumerable<MergeRequestModel> pendingMergeRequests)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        ArgumentNullException.ThrowIfNull(pendingMergeRequests);

        if (tataRekening.Status != TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                "Financial Verification hanya dapat dilakukan setelah Billing berstatus CLOSED.");

        if (tataRekening.Status is TataRekeningStatusEnum.Finalized or TataRekeningStatusEnum.Lunas)
            throw new InvalidOperationException(
                "Financial Verification tidak dapat dilakukan pada billing yang sudah FINALIZED atau LUNAS.");

        var regId = tataRekening.RegId;
        var hasPendingMerge = pendingMergeRequests.Any(m =>
            m.Status == MergeRequestStatusEnum.Pending &&
            (string.Equals(m.SourceRegId, regId, StringComparison.Ordinal) ||
             string.Equals(m.TargetRegId, regId, StringComparison.Ordinal)));

        if (hasPendingMerge)
            throw new InvalidOperationException(
                "Financial Verification tidak dapat dimulai selama masih terdapat Merge Request berstatus Pending.");

        tataRekening.CompleteFinancialVerification(petugasVerif, verifiedAt);
    }

    public void RequireAdjustment(TataRekeningModel tataRekening)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        tataRekening.RequireFinancialAdjustment();
    }

    public void ResetVerification(TataRekeningModel tataRekening)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        tataRekening.ResetFinancialVerification();
    }
}
