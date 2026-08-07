using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StockPositionDto(
    string BrgId,
    string ReceiptSourceId,
    string LayananId,
    long Version,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static StockPositionDto FromModel(StockPositionModel model)
        => new(
            model.BrgId,
            model.ReceiptSourceId,
            model.LayananId,
            model.Version,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate);

    public StockPositionModel ToModel(IEnumerable<StockLayerModel> layers)
        => StockPositionModel.Create(
            StockWriteScopeKeyType.Create(BrgId, ReceiptSourceId, LayananId),
            layers,
            Version);
}
