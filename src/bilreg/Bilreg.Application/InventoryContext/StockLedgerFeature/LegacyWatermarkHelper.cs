using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Catch-up / stale predicate using port-mapped <see cref="LegacyStockJournalReadModel.TglMutasi"/>
/// and <see cref="LegacyStockJournalReadModel.LegacyBukuId"/> (architecture §8; ADR-STL-005).
/// Do not compose datetime in SQL separately from <c>ILegacyStockReadPort</c>.
/// </summary>
public static class LegacyWatermarkHelper
{
    public static bool IsAfterWatermark(
        LegacyStockJournalReadModel journal,
        DateTime tglMutasiLast,
        string lastLegacyBukuId)
    {
        // C1 empty hydrate uses EmptyDate + empty buku id as "nothing synced yet"
        // (ADR-STL-003 sentinel). Real journal datetimes are never > 3000-01-01, so
        // treat absent watermark as every journal being after-watermark.
        if (tglMutasiLast == StockLedgerSentinel.EmptyDate &&
            string.IsNullOrEmpty(lastLegacyBukuId))
            return true;

        if (journal.TglMutasi > tglMutasiLast)
            return true;

        if (journal.TglMutasi == tglMutasiLast &&
            string.Compare(journal.LegacyBukuId, lastLegacyBukuId ?? string.Empty, StringComparison.Ordinal) > 0)
            return true;

        return false;
    }

    public static bool IsAfterWatermark(
        LegacyStockJournalReadModel journal,
        StockLegacyScopeModel scope) =>
        IsAfterWatermark(journal, scope.TglMutasiLast, scope.LastLegacyBukuId);

    public static IReadOnlyList<LegacyStockJournalReadModel> FilterAfterWatermark(
        IEnumerable<LegacyStockJournalReadModel> journals,
        DateTime tglMutasiLast,
        string lastLegacyBukuId) =>
        journals
            .Where(j => IsAfterWatermark(j, tglMutasiLast, lastLegacyBukuId))
            .ToList();

    public static IReadOnlyList<LegacyStockJournalReadModel> FilterAfterWatermark(
        IEnumerable<LegacyStockJournalReadModel> journals,
        StockLegacyScopeModel scope) =>
        FilterAfterWatermark(journals, scope.TglMutasiLast, scope.LastLegacyBukuId);

    public static bool HasLegacyRowsBeyondWatermark(
        IEnumerable<LegacyStockJournalReadModel> journals,
        StockLegacyScopeModel scope) =>
        journals.Any(j => IsAfterWatermark(j, scope));
}
