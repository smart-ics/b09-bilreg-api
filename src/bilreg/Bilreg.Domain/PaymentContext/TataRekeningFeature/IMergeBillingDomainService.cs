using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public interface IMergeBillingDomainService
{
    MergeBillingResult Execute(
        MergeRequestModel mergeRequest,
        TataRekeningModel sourceTataRekening,
        TataRekeningModel targetTataRekening);

    MergeBillingResult Execute(
        MergeRequestModel mergeRequest,
        TataRekeningModel sourceTataRekening,
        TataRekeningModel targetTataRekening,
        RegModel sourceReg,
        RegModel targetReg);

    void ValidatePatientIdentity(RegModel sourceReg, RegModel targetReg);

    void ValidatePatientIdentity(string? sourceNoRm, string? targetNoRm);

    void ValidateAutoMergeIntegrity(
        RegModel sourceReg,
        RegModel targetReg,
        TataRekeningModel? sourceTataRekening = null,
        TataRekeningModel? targetTataRekening = null);
}
