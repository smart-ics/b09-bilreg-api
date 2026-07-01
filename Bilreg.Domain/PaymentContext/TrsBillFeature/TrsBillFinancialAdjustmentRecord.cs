using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

/// <summary>
/// Financial-layer adjustment applied without mutating operational charge-source transaction events.
/// </summary>
public record TrsBillFinancialAdjustmentRecord(
    FinancialAdjustmentTypeEnum Type,
    decimal Amount,
    string Reason,
    DateTime AppliedAt,
    string? SubsidyPayerId = null);
