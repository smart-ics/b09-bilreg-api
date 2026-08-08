namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P5-S3 — SourceConsequence idempotency key for Native Stock Transfer.
/// One key per MT source-transaction responsibility (whole mutasi / <c>fs_kd_mutasi</c>),
/// independent of line order, BrgId set, or line count. Not per line and not per Item.
/// Discovery continues to ignore non-<c>SYNC|…</c> keys (Phase 3 behavior unchanged).
/// </summary>
public static class TransferConsequenceIdempotency
{
    public static string BuildSourceConsequenceKey(string sourceTransactionId)
    {
        if (string.IsNullOrWhiteSpace(sourceTransactionId))
            throw new ArgumentException("SourceTransactionId is required.", nameof(sourceTransactionId));

        return $"MT|{sourceTransactionId.Trim()}";
    }
}
