using Bilreg.Domain.PaymentContext.PasienBalanceFeature;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public record BilrgTataRekPasienBalanceDto(
    string PasienId,
    decimal CurrentJasaBalance,
    decimal CurrentObatBalance,
    string LastHistoryId,
    DateTime UpdatedAt,
    int Version,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static BilrgTataRekPasienBalanceDto FromModelForInsert(PasienBalanceModel model)
    {
        var updDate = model.UpdatedAt;
        return new BilrgTataRekPasienBalanceDto(
            model.PasienId,
            model.CurrentJasaBalance,
            model.CurrentObatBalance,
            model.LastHistoryId == "-" ? string.Empty : model.LastHistoryId,
            model.UpdatedAt,
            model.Version,
            model.LastModifiedBy,
            updDate,
            model.LastModifiedBy,
            updDate,
            string.Empty,
            EmptyDate);
    }

    public static BilrgTataRekPasienBalanceDto FromModelForUpdate(PasienBalanceModel model) =>
        new(
            model.PasienId,
            model.CurrentJasaBalance,
            model.CurrentObatBalance,
            model.LastHistoryId == "-" ? string.Empty : model.LastHistoryId,
            model.UpdatedAt,
            model.Version,
            string.Empty,
            EmptyDate,
            model.LastModifiedBy,
            model.UpdatedAt == EmptyDate ? EmptyDate : model.UpdatedAt,
            string.Empty,
            EmptyDate);

    public PasienBalanceModel ToModel(IEnumerable<PasienBalanceHistoryType> listHistory)
    {
        var history = listHistory.ToList();
        return PasienBalanceModel.Hydrate(
            PasienId,
            CurrentJasaBalance,
            CurrentObatBalance,
            string.IsNullOrEmpty(LastHistoryId) ? "-" : LastHistoryId,
            UpdatedAt,
            Version,
            UpdUser,
            history.Select(x => x with { IsPersisted = true }));
    }
}
