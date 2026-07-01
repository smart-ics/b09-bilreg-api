using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public record LegacyOutstandingReceivable(
    string PasienId,
    string RegId,
    decimal OutstandingJasa,
    decimal OutstandingObat,
    DateTime LastTransactionDate,
    string SourceReference);
