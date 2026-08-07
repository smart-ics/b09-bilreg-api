using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Persistence gateway for Item + Receipt Source coexistence state shapes.
/// Does not discover legacy changes or compute fingerprints.
/// </summary>
public interface IStockLedgerScopeStateRepo :
    ISaveChange<StockLedgerScopeStateModel>,
    ILoadEntity<StockLedgerScopeStateModel, IStockLedgerScopeKey>
{
    /// <summary>
    /// P2-S5 — insert a new Scope row. Returns <c>false</c> when a concurrent insert
    /// already owns the primary key (fail closed; caller reloads).
    /// </summary>
    bool TryInsertNew(StockLedgerScopeStateModel model);

    /// <summary>
    /// P2-S5 — conditional update for reconstruction claim. Succeeds only when the
    /// durable ReconstructionStatus still equals <paramref name="expectedPriorStatus"/>.
    /// </summary>
    bool TryUpdateWhenReconstructionStatus(
        StockLedgerScopeStateModel model,
        ReconstructionStatusEnum expectedPriorStatus);
}
