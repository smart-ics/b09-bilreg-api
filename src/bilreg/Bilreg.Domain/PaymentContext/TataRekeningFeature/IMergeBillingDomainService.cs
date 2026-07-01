namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public interface IMergeBillingDomainService
{
    MergeBillingResult Execute(
        MergeRequestModel mergeRequest,
        TataRekeningModel sourceTataRekening,
        TataRekeningModel targetTataRekening);
}
