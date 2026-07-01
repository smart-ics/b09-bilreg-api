using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record TrsBillSummaryDto(
    string TrsBillingId,
    string RegId,
    BillModulGroup ModulGroup,
    decimal NilaiTotal,
    decimal FinancialTotal);
