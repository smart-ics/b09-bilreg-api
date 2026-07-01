namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public interface IFinancialAdjustmentDomainService
{
    FinancialAdjustmentResult Apply(
        TataRekeningModel tataRekening,
        FinancialAdjustmentRequest request,
        DateTime appliedAt);
}
