using Bilreg.Domain.PaymentContext.PasienBalanceFeature;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public record BilrgTataRekPasienBalanceDto(
    string PasienId,
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
        var auditUser = ResolveAuditUser(model);
        var now = DateTime.Now;
        return new BilrgTataRekPasienBalanceDto(
            model.PasienId,
            model.Version,
            auditUser,
            now,
            auditUser,
            now,
            string.Empty,
            EmptyDate);
    }

    public static BilrgTataRekPasienBalanceDto FromModelForUpdate(PasienBalanceModel model) =>
        new(
            model.PasienId,
            model.Version,
            string.Empty,
            EmptyDate,
            ResolveAuditUser(model),
            DateTime.Now,
            string.Empty,
            EmptyDate);

    public PasienBalanceModel ToModel(IEnumerable<OutstandingEntryType> outstandingEntries)
    {
        var entries = outstandingEntries.ToList();
        return PasienBalanceModel.Hydrate(
            PasienId,
            Version,
            entries.Select(x => x with { IsPersisted = true }));
    }

    private static string ResolveAuditUser(PasienBalanceModel model)
    {
        var entry = model.OutstandingEntries.FirstOrDefault();
        return string.IsNullOrWhiteSpace(entry?.CreatedBy) ? "system" : entry.CreatedBy;
    }
}
