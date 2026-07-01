using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public sealed record FinancialAdjustmentRequest(
    FinancialAdjustmentTypeEnum Type,
    decimal Amount,
    string Reason,
    string? TrsBillingId = null,
    PaymentType? SubsidyPayer = null,
    bool RequiresChargeSourceChange = false,
    TrsBillType? ManualChargeBill = null);
