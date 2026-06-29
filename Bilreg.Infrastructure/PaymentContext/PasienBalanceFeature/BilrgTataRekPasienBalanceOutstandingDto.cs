using Bilreg.Domain.PaymentContext.PasienBalanceFeature;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public record BilrgTataRekPasienBalanceOutstandingDto(
    string EntryId,
    string PasienId,
    string RegId,
    decimal OutstandingJasa,
    decimal OutstandingObat,
    DateTime LastTransactionDate,
    string SourceReference,
    DateTime CreatedAt,
    string CreatedBy)
{
    public static BilrgTataRekPasienBalanceOutstandingDto FromModel(OutstandingEntryType model) =>
        new(
            model.EntryId,
            model.PasienId,
            model.RegId,
            model.OutstandingJasa,
            model.OutstandingObat,
            model.LastTransactionDate,
            model.SourceReference,
            model.CreatedAt,
            model.CreatedBy);

    public OutstandingEntryType ToModel() =>
        new(
            EntryId,
            PasienId,
            RegId,
            OutstandingJasa,
            OutstandingObat,
            LastTransactionDate,
            SourceReference,
            CreatedAt,
            CreatedBy,
            isPersisted: true);
}
