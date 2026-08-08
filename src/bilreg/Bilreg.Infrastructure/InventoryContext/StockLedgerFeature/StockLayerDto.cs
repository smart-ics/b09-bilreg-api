using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StockLayerDto(
    string StockLayerId,
    string BrgId,
    string ReceiptSourceId,
    string LayananId,
    string LayerFormingMovementId,
    decimal InitialQuantity,
    decimal RemainingQuantity,
    decimal AmountPerUnit,
    DateTime ExpirationDate,
    DateTime EffectiveReceiptTime,
    int Origin,
    string Batch,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static StockLayerDto FromModel(StockLayerModel model)
        => new(
            model.StockLayerId,
            model.BrgId,
            model.ReceiptSourceId,
            model.LayananId,
            model.LayerFormingMovementId,
            model.InitialQuantity,
            model.RemainingQuantity,
            model.UnitValuation.AmountPerUnit,
            StockLedgerPersistenceSentinel.ExpirationToStorage(model.ExpirationDate),
            model.EffectiveReceiptTime,
            (int)model.Origin,
            StockLedgerPersistenceSentinel.NullToEmpty(model.Batch),
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate);

    public StockLayerModel ToModel()
        => StockLayerModel.Rehydrate(
            StockLayerId,
            BrgId,
            ReceiptSourceId,
            LayananId,
            LayerFormingMovementId,
            InitialQuantity,
            RemainingQuantity,
            UnitValuationType.Create(AmountPerUnit),
            EffectiveReceiptTime,
            (StockFactOriginEnum)Origin,
            StockLedgerPersistenceSentinel.ExpirationFromStorage(ExpirationDate),
            StockLedgerPersistenceSentinel.EmptyToNull(Batch));
}
