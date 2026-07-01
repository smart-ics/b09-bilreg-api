namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public sealed record MergeBillingResult(
    MergeRequestModel MergeRequest,
    TataRekeningModel SourceTataRekening,
    TataRekeningModel TargetTataRekening);
