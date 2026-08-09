using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

public interface IStockBatchRepo :
    ISaveChange<StockBatchModel>,
    ILoadEntity<StockBatchModel, IStockBatchKey>
{
    MayBe<StockBatchModel> LoadByNaturalKey(string brgId, string brgMasukReffId);

    /// <summary>
    /// Candidates only (QtySisa &gt; 0). Ordering for FEFO/FIFO is owned by S1-B domain allocator.
    /// </summary>
    IEnumerable<LocationStockBalanceModel> ListAllocationCandidates(string brgId, string layananId);
}
