using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public record StockMovementLineDto(
    string StockMovementId,
    int LineNo,
    string BrgId,
    string ReceiptSourceId,
    string LayananId,
    int Direction,
    decimal Quantity,
    decimal AmountPerUnit,
    int Origin,
    string StockLayerId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    public static StockMovementLineDto FromModel(string stockMovementId, StockMovementLineType line)
        => new(
            stockMovementId,
            line.LineNo,
            line.BrgId,
            line.ReceiptSourceId,
            line.LayananId,
            (int)line.Direction,
            line.Quantity,
            line.UnitValuation.AmountPerUnit,
            (int)line.Origin,
            StockLedgerPersistenceSentinel.NullToEmpty(line.StockLayerId),
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate,
            string.Empty,
            StockLedgerPersistenceSentinel.EmptyDate);

    public StockMovementLineType ToModel()
        => new(
            LineNo,
            BrgId,
            ReceiptSourceId,
            LayananId,
            (StockMovementDirectionEnum)Direction,
            Quantity,
            UnitValuationType.Create(AmountPerUnit),
            (StockFactOriginEnum)Origin,
            StockLedgerPersistenceSentinel.EmptyToNull(StockLayerId));
}
