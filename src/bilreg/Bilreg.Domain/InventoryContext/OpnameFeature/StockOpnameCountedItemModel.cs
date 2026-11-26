using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public class StockOpnameCountedItemModel
{
    #region PROPERTIES
    public string CountedItemId { get; private set; }
    public string SessionId { get; private set; }
    public string BrgId { get; private set; }
    public string BatchNo { get; private set; }
    public string ExpiredDate { get; private set; }
    public string BaseSatuanId { get; private set; }
    public decimal TotalCountQty { get; private set; }
    public DateTime LastCountTime { get; private set; }

    public ItemTupleKey TupleKey => new(BrgId, BatchNo, ExpiredDate);
    #endregion

    #region CREATION
    public StockOpnameCountedItemModel(
        string countedItemId,
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string baseSatuanId,
        decimal totalCountQty,
        DateTime lastCountTime)
    {
        CountedItemId = countedItemId?.Trim() ?? string.Empty;
        SessionId = sessionId?.Trim() ?? string.Empty;
        BrgId = brgId?.Trim() ?? string.Empty;
        BatchNo = batchNo?.Trim() ?? string.Empty;
        ExpiredDate = expiredDate?.Trim() ?? string.Empty;
        BaseSatuanId = baseSatuanId?.Trim() ?? string.Empty;
        TotalCountQty = totalCountQty;
        LastCountTime = lastCountTime;
    }

    public static StockOpnameCountedItemModel Create(
        string countedItemId,
        string sessionId,
        string brgId,
        string batchNo,
        string expiredDate,
        string baseSatuanId,
        decimal totalCountQty,
        DateTime lastCountTime)
    {
        Guard.Against.NullOrWhiteSpace(sessionId, nameof(sessionId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        return new StockOpnameCountedItemModel(
            countedItemId,
            sessionId,
            brgId,
            batchNo,
            expiredDate,
            baseSatuanId,
            totalCountQty,
            lastCountTime);
    }
    #endregion
}
