namespace Bilreg.Domain.InventoryContext.OpnameFeature;

public record ItemTupleKey
{
    public string BrgId { get; init; }
    public string BatchNo { get; init; }
    public string ExpiredDate { get; init; }

    public ItemTupleKey(string brgId, string batchNo, string expiredDate)
    {
        BrgId = brgId?.Trim() ?? string.Empty;
        BatchNo = batchNo?.Trim() ?? string.Empty;
        ExpiredDate = expiredDate?.Trim() ?? string.Empty;
    }

    public static ItemTupleKey Create(string brgId, string batchNo, string expiredDate) =>
        new(brgId, batchNo, expiredDate);
}
