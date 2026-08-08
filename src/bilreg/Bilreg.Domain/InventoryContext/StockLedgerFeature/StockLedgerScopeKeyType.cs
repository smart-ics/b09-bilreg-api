using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Reconstruction / reconciliation scope: Item (<see cref="IBrgKey.BrgId"/>)
/// + Receipt Source across all Stock Locations.
/// This is not a write boundary and does not encode runtime authority.
/// </summary>
public record StockLedgerScopeKeyType : IStockLedgerScopeKey
{
    #region CREATION
    public StockLedgerScopeKeyType(string brgId, string receiptSourceId)
    {
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        BrgId = brgId;
        ReceiptSourceId = receiptSourceId;
    }

    public static StockLedgerScopeKeyType Create(string brgId, string receiptSourceId)
        => new(brgId, receiptSourceId);

    public static StockLedgerScopeKeyType Create(IBrgKey item, IReceiptSourceKey receiptSource)
        => new(item.BrgId, receiptSource.ReceiptSourceId);

    public static StockLedgerScopeKeyType Default => new("-", "-");

    public static IStockLedgerScopeKey Key(string brgId, string receiptSourceId)
        => Default with { BrgId = brgId, ReceiptSourceId = receiptSourceId };
    #endregion

    #region PROPERTIES
    public string BrgId { get; init; }
    public string ReceiptSourceId { get; init; }
    #endregion
}

public interface IStockLedgerScopeKey : IBrgKey, IReceiptSourceKey
{
}
