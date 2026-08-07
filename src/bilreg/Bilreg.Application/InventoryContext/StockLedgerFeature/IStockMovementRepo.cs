using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Persistence gateway for immutable Stock Movement aggregates (header + lines).
/// Completed movements are insert-once; mutation is rejected.
/// </summary>
public interface IStockMovementRepo :
    ISaveChange<StockMovementModel>,
    ILoadEntity<StockMovementModel, IStockMovementKey>
{
}
