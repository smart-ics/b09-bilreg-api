using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public class PasienBalanceBootstrapService
{
    public const string BootstrapUser = "bootstrap";

    private readonly IPasienBalanceLegacyReader _legacyReader;

    public PasienBalanceBootstrapService(IPasienBalanceLegacyReader legacyReader)
    {
        _legacyReader = legacyReader;
    }

    public PasienBalanceModel Bootstrap(IPasienKey key)
    {
        var legacyRows = _legacyReader.ListOutstanding(key).ToList();
        var model = PasienBalanceModel.Create(key.PasienId);

        var entries = legacyRows
            .Where(x => x.OutstandingJasa + x.OutstandingObat > 0m)
            .Select(x => new OutstandingEntryType(
                entryId: string.Empty,
                pasienId: key.PasienId,
                regId: x.RegId,
                outstandingJasa: x.OutstandingJasa,
                outstandingObat: x.OutstandingObat,
                lastTransactionDate: x.LastTransactionDate,
                sourceReference: x.SourceReference,
                createdAt: DateTime.MinValue,
                createdBy: string.Empty,
                isPersisted: false))
            .ToList();

        if (entries.Count > 0)
            model.ReplaceOutstandingEntries(entries, BootstrapUser);

        return model;
    }
}
