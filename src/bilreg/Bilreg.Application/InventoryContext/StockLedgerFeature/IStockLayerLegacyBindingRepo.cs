using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Persistence gateway for the StockLayerId ↔ LegacyRowId coexistence projection.
/// </summary>
public interface IStockLayerLegacyBindingRepo
{
    void SaveChanges(StockLayerLegacyBindingType binding);

    MayBe<StockLayerLegacyBindingType> LoadByStockLayerId(string stockLayerId);

    IReadOnlyList<StockLayerLegacyBindingType> ListByLedgerScope(IStockLedgerScopeKey scope);

    IReadOnlyList<StockLayerLegacyBindingType> ListByWriteScope(IStockWriteScopeKey writeScope);
}
