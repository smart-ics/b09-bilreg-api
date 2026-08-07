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
}
