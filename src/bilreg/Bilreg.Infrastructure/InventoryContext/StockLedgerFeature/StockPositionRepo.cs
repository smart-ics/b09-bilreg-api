using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public class StockPositionRepo : IStockPositionRepo
{
    private readonly IStockPositionDal _positionDal;
    private readonly IStockLayerDal _layerDal;

    public StockPositionRepo(IStockPositionDal positionDal, IStockLayerDal layerDal)
    {
        _positionDal = positionDal;
        _layerDal = layerDal;
    }

    public void SaveChanges(StockPositionModel model)
    {
        var stored = _positionDal.GetData(model);
        var dto = StockPositionDto.FromModel(model);

        if (stored is null)
        {
            // First durable write may already carry Version > 0 when layers were
            // established in-memory (CreateEmpty → AddLayer) before persistence.
            _positionDal.Insert(dto);
            PersistLayers(model, existingIds: []);
            return;
        }

        // Domain bumps Version on mutation; persist requires exactly stored.Version + 1.
        if (model.Version != stored.Version + 1)
        {
            throw StockLedgerPersistenceException.Concurrency(
                $"Stock Position ({model.BrgId}/{model.ReceiptSourceId}/{model.LayananId}) version must be " +
                $"{stored.Version + 1}, not {model.Version}.");
        }

        if (_positionDal.UpdateConditional(dto, stored.Version) != 1)
        {
            throw StockLedgerPersistenceException.Concurrency(
                $"Stock Position ({model.BrgId}/{model.ReceiptSourceId}/{model.LayananId}) " +
                "was changed by another process.");
        }

        var existingIds = (_layerDal.ListData(model) ?? [])
            .Select(x => x.StockLayerId)
            .ToHashSet(StringComparer.Ordinal);
        PersistLayers(model, existingIds);
    }

    public MayBe<StockPositionModel> LoadEntity(IStockWriteScopeKey key)
    {
        var header = _positionDal.GetData(key);
        if (header is null)
            return MayBe<StockPositionModel>.None;

        var layers = (_layerDal.ListData(key) ?? [])
            .Select(x => x.ToModel())
            .ToArray();

        return MayBe.From(header.ToModel(layers));
    }

    private void PersistLayers(StockPositionModel model, HashSet<string> existingIds)
    {
        foreach (var layer in model.Layers)
        {
            var dto = StockLayerDto.FromModel(layer);
            if (existingIds.Contains(layer.StockLayerId))
                _layerDal.Update(dto);
            else
                _layerDal.Insert(dto);
        }
    }
}
