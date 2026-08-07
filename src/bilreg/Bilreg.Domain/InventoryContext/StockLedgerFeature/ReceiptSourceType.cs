using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Receipt Source identity. In the legacy stock model this is KodeDO (<c>fs_kd_do</c>).
/// Origin/authority are intentionally not represented here.
/// </summary>
public record ReceiptSourceType : IReceiptSourceKey
{
    #region CREATION
    public ReceiptSourceType(string receiptSourceId)
    {
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        ReceiptSourceId = receiptSourceId;
    }

    public static ReceiptSourceType Create(string receiptSourceId)
        => new(receiptSourceId);

    public static ReceiptSourceType Default => new("-");

    public static IReceiptSourceKey Key(string receiptSourceId)
        => Default with { ReceiptSourceId = receiptSourceId };
    #endregion

    #region PROPERTIES
    public string ReceiptSourceId { get; init; }
    #endregion
}

public interface IReceiptSourceKey
{
    string ReceiptSourceId { get; }
}
