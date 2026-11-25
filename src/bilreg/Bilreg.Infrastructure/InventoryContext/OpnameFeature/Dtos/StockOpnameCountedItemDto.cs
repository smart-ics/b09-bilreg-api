using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;

public record StockOpnameCountedItemDto(
    string SessionId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    string BaseSatuanId,
    decimal TotalCountQty,
    DateTime LatestCountTime,
    int EntryCount = 0)
{
    // Computed property for backwards compatibility with legacy callers
    public string CountedItemId => $"{SessionId}_{BrgId}_{BatchNo}_{ExpiredDate}";

    // Backwards compatibility constructor for tests / legacy callers until P2-S03
    [Obsolete("Use primary constructor matching FARPU_StockOpnameCountedItem schema")]
    public StockOpnameCountedItemDto(
        string countedItemId,
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string baseSatuanId,
        decimal totalCountQty,
        DateTime lastCountTime)
        : this(sessionId, brgId, batchNo, expiredDate, baseSatuanId, totalCountQty, lastCountTime, 0)
    {
    }

    public static StockOpnameCountedItemDto FromModel(StockOpnameCountedItemModel model) =>
        new(
            SessionId: model.SessionId,
            BrgId: model.BrgId,
            BatchNo: model.BatchNo,
            ExpiredDate: model.ExpiredDate,
            BaseSatuanId: model.BaseSatuanId,
            TotalCountQty: model.TotalCountQty,
            LatestCountTime: model.LastCountTime,
            EntryCount: 0
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
            lastCountTime: LatestCountTime
        );
}
