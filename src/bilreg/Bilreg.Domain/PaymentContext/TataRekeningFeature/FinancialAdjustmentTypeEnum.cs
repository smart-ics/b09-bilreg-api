namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public enum FinancialAdjustmentTypeEnum
{
    ManualCharge = 0,
    BillingCorrection = 1,
    Waive = 2,
    Subsidy = 3,
    MergeBillingCorrection = 4,
}
