using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

public interface IStockLegacyBindingRepo
{
    void Insert(StockLegacyBindingModel model);
    void Insert(StockLegacyBindingModel model, string userId);
    MayBe<StockLegacyBindingModel> FindByLegacyBukuId(string legacyBukuId);
    MayBe<StockLegacyBindingModel> FindByStokMutasiId(string stokMutasiId);
}
