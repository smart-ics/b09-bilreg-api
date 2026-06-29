using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public record PasienBalanceValidationResult(
    bool IsValid,
    string Message,
    decimal LegacyJasaBalance,
    decimal LegacyObatBalance,
    decimal StoredJasaBalance,
    decimal StoredObatBalance);
