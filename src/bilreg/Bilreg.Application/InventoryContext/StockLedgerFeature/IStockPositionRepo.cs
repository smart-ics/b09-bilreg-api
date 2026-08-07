using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Persistence gateway for Stock Position write-scope state (header + layers).
/// Updates are conditioned on the optimistic concurrency Version token.
/// Depleted layers are retained.
/// </summary>
public interface IStockPositionRepo :
    ISaveChange<StockPositionModel>,
    ILoadEntity<StockPositionModel, IStockWriteScopeKey>
{
}
