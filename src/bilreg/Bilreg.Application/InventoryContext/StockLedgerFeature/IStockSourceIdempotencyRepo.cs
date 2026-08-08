using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Persistence gateway for unique source consequence / sync batch keys (G-07).
/// Second insert of the same business key returns the existing row without mutation.
/// </summary>
public interface IStockSourceIdempotencyRepo :
    ILoadEntity<StockSourceIdempotencyModel, IStockSourceIdempotencyKey>
{
    MayBe<StockSourceIdempotencyModel> LoadByBusinessKey(IStockSourceIdempotencyBusinessKey key);

    /// <summary>
    /// Inserts the idempotency row. When (Kind, Key) already exists, returns the
    /// stored row and <see cref="StockSourceIdempotencyInsertResult.WasInserted"/> = false.
    /// </summary>
    StockSourceIdempotencyInsertResult InsertOrGetExisting(StockSourceIdempotencyModel model);

    /// <summary>
    /// P3-S4 — Ledger-known SyncBatch / SourceConsequence identity records for one
    /// Reconstruction Scope (keys starting with <c>SYNC|</c>).
    /// </summary>
    IReadOnlyList<StockSourceIdempotencyModel> ListSyncIdentityRecordsForScope(IStockLedgerScopeKey scope);
}

public sealed record StockSourceIdempotencyInsertResult(
    bool WasInserted,
    StockSourceIdempotencyModel Record);
