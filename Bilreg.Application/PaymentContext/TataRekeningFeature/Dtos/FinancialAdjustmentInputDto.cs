using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record FinancialAdjustmentInputDto(
    FinancialAdjustmentTypeEnum Type,
    decimal Amount,
    string Reason,
    string? TrsBillingId = null,
    string? SubsidyPaymentId = null,
    string? SubsidyPaymentName = null,
    bool RequiresChargeSourceChange = false,
    string? ManualChargeTrsBillingId = null);
