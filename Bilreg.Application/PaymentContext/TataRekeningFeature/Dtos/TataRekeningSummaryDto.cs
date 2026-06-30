using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record TataRekeningSummaryDto(
    string RegId,
    TataRekeningStatusEnum Status,
    FinancialVerificationStatusEnum FinancialVerificationStatus,
    bool IsFinancialResponsibilityAllocated,
    bool SettlementInitiated);
