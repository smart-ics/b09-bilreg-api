using Bilreg.Domain.PaymentContext.PasienBalanceFeature;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public record BilrgTataRekPasienBalanceHistoryDto(
    string HistoryId,
    string PasienId,
    string RegId,
    DateTime TrsDate,
    decimal OpeningJasaBalance,
    decimal OpeningObatBalance,
    decimal ChargeJasa,
    decimal ChargeObat,
    decimal PaymentJasa,
    decimal PaymentObat,
    decimal ClosingJasaBalance,
    decimal ClosingObatBalance,
    string Remarks,
    DateTime CreatedAt,
    string CreatedBy)
{
    public static BilrgTataRekPasienBalanceHistoryDto FromModel(PasienBalanceHistoryType model) =>
        new(
            model.HistoryId,
            model.PasienId,
            model.RegId,
            model.TrsDate,
            model.OpeningJasaBalance,
            model.OpeningObatBalance,
            model.ChargeJasa,
            model.ChargeObat,
            model.PaymentJasa,
            model.PaymentObat,
            model.ClosingJasaBalance,
            model.ClosingObatBalance,
            model.Remarks,
            model.CreatedAt,
            model.CreatedBy);

    public PasienBalanceHistoryType ToModel() =>
        new(
            HistoryId,
            PasienId,
            RegId,
            TrsDate,
            OpeningJasaBalance,
            OpeningObatBalance,
            ChargeJasa,
            ChargeObat,
            PaymentJasa,
            PaymentObat,
            ClosingJasaBalance,
            ClosingObatBalance,
            Remarks,
            CreatedAt,
            CreatedBy,
            isPersisted: true);
}
