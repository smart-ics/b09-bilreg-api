using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public sealed record FinancialAdjustmentResult(
    bool RequiresReopen,
    FinancialAdjustmentTypeEnum Type,
    string? TrsBillingId,
    TrsBillFinancialAdjustmentRecord? AppliedAdjustment = null);
