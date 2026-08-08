using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S2 — SourceConsequence idempotency key for Native DO Receipt.
/// One key per source-transaction responsibility (whole DO), not per line.
/// Discovery continues to ignore non-<c>SYNC|…</c> keys (Phase 3 behavior unchanged).
/// </summary>
public static class DoReceiptConsequenceIdempotency
{
    public static string BuildSourceConsequenceKey(
        IStockLedgerScopeKey scope,
        string sourceTransactionId)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (string.IsNullOrWhiteSpace(scope.BrgId))
            throw new ArgumentException("BrgId is required.", nameof(scope));
        if (string.IsNullOrWhiteSpace(scope.ReceiptSourceId))
            throw new ArgumentException("ReceiptSourceId is required.", nameof(scope));
        if (string.IsNullOrWhiteSpace(sourceTransactionId))
            throw new ArgumentException("SourceTransactionId is required.", nameof(sourceTransactionId));

        return $"DO|{scope.BrgId.Trim()}|{scope.ReceiptSourceId.Trim()}|{sourceTransactionId.Trim()}";
    }
}
