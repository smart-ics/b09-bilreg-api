using Bilreg.Application.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StockLayerLegacyBindingDto(
    string StockLayerId,
    string LegacyRowId,
    string BrgId,
    string ReceiptSourceId,
    string LayananId,
    decimal AmountPerUnit,
    DateTime ExpirationDate,
    string Batch,
    DateTime BoundAt,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static StockLayerLegacyBindingDto FromModel(StockLayerLegacyBindingType model)
        => new(
            model.StockLayerId,
            model.LegacyRowId,
            model.BrgId,
            model.ReceiptSourceId,
            model.LayananId,
            model.AmountPerUnit,
            StockLedgerPersistenceSentinel.ExpirationToStorage(model.ExpirationDate),
            StockLedgerPersistenceSentinel.NullToEmpty(model.Batch),
            model.BoundAt,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate);

    public StockLayerLegacyBindingType ToModel()
        => new(
            StockLayerId,
            LegacyRowId,
            BrgId,
            ReceiptSourceId,
            LayananId,
            AmountPerUnit,
            StockLedgerPersistenceSentinel.ExpirationFromStorage(ExpirationDate),
            StockLedgerPersistenceSentinel.EmptyToNull(Batch),
            BoundAt);
}
