using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

public interface IStockLegacyBindingRepo
{
    void Insert(StockLegacyBindingModel model);
    MayBe<StockLegacyBindingModel> FindByLegacyBukuId(string legacyBukuId);
    MayBe<StockLegacyBindingModel> FindByStokMutasiId(string stokMutasiId);
}
