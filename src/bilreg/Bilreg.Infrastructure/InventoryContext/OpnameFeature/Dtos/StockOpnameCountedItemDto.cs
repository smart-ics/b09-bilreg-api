using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameCountedItemDto(
    string CountedItemId,
    string SessionId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    string BaseSatuanId,
    decimal TotalCountQty,
    DateTime LastCountTime)
{
    public static StockOpnameCountedItemDto FromModel(StockOpnameCountedItemModel model) =>
        new(
            CountedItemId: model.CountedItemId,
            SessionId: model.SessionId,
            BrgId: model.BrgId,
            BatchNo: model.BatchNo,
            ExpiredDate: model.ExpiredDate,
            BaseSatuanId: model.BaseSatuanId,
            TotalCountQty: model.TotalCountQty,
            LastCountTime: model.LastCountTime
        );

    public StockOpnameCountedItemModel ToModel() =>
        new(
            countedItemId: CountedItemId,
            sessionId: SessionId,
            brgId: BrgId,
            batchNo: BatchNo,
            expiredDate: ExpiredDate,
            baseSatuanId: BaseSatuanId,
            totalCountQty: TotalCountQty,
            lastCountTime: LastCountTime
        );
}
