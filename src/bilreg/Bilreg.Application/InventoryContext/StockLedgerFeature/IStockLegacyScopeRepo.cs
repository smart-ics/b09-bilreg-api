using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

public interface IStockLegacyScopeRepo :
    ISaveChange<StockLegacyScopeModel>,
    ILoadEntity<StockLegacyScopeModel, IStockLegacyScopeKey>
{
    /// <summary>Same as <see cref="ISaveChange{T}.SaveChanges"/> with explicit audit user.</summary>
    void SaveChanges(StockLegacyScopeModel model, string userId);
}
