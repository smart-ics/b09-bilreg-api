namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Kind of unique Stock Ledger source key (G-07).
/// Persisted as INT on <c>BILRG_StokSourceIdempotency.IdempotencyKind</c>.
/// </summary>
public enum StockSourceIdempotencyKindEnum
{
    SourceConsequence = 1,
    SyncBatch = 2,
    /// <summary>
    /// Idempotency for a completed Initial Reconstruction baseline of one Item + Receipt Source.
    /// Distinct from FO <see cref="SourceConsequence"/> keys so reconstruction cannot collide with later native writes.
    /// </summary>
    ReconstructionBaseline = 3
}
