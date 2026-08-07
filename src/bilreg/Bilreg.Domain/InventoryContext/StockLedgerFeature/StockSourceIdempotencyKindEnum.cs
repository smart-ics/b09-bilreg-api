namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Kind of unique Stock Ledger source key (G-07).
/// Persisted as INT on <c>BILRG_StokSourceIdempotency.IdempotencyKind</c>.
/// </summary>
public enum StockSourceIdempotencyKindEnum
{
    SourceConsequence = 1,
    SyncBatch = 2
}
