using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

public interface IStockLegacyScopeRepo :
    ISaveChange<StockLegacyScopeModel>,
    ILoadEntity<StockLegacyScopeModel, IStockLegacyScopeKey>
{
}
