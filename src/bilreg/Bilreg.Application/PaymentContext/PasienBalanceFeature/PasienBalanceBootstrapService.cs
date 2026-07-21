using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public class PasienBalanceBootstrapService
{
    public const string BootstrapUser = "bootstrap";

    private readonly IPasienBalanceLegacyReader _legacyReader;
    private readonly ITglJamProvider _tglJamProvider;

    public PasienBalanceBootstrapService(IPasienBalanceLegacyReader legacyReader,
        ITglJamProvider tglJamProvider)
    {
        _legacyReader = legacyReader;
        _tglJamProvider = tglJamProvider;
    }

    public PasienBalanceModel Bootstrap(IPasienKey key)
    {
        var occurredAt = _tglJamProvider.Now;
        var legacyRows = _legacyReader.ListOutstanding(key, DateOnly.FromDateTime(occurredAt)).ToList();
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
            model.ReplaceOutstandingEntries(entries, BootstrapUser, occurredAt);

        return model;
    }
}
