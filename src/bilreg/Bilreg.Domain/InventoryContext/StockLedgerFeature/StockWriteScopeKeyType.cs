using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Candidate write-consistency scope: Item (<see cref="IBrgKey.BrgId"/>)
/// + Receipt Source + Stock Location (<see cref="ILayananKey.LayananId"/>).
/// Distinct from reconstruction/reconciliation scope. Does not encode runtime authority.
/// </summary>
public record StockWriteScopeKeyType : IStockWriteScopeKey
{
    #region CREATION
    public StockWriteScopeKeyType(string brgId, string receiptSourceId, string layananId)
    {
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        BrgId = brgId;
        ReceiptSourceId = receiptSourceId;
        LayananId = layananId;
    }

    public static StockWriteScopeKeyType Create(string brgId, string receiptSourceId, string layananId)
        => new(brgId, receiptSourceId, layananId);

    public static StockWriteScopeKeyType Create(
        IBrgKey item,
        IReceiptSourceKey receiptSource,
        ILayananKey stockLocation)
        => new(item.BrgId, receiptSource.ReceiptSourceId, stockLocation.LayananId);

    public static StockWriteScopeKeyType Default => new("-", "-", "-");

    public static IStockWriteScopeKey Key(string brgId, string receiptSourceId, string layananId)
        => Default with
        {
            BrgId = brgId,
            ReceiptSourceId = receiptSourceId,
            LayananId = layananId
        };
    #endregion

    #region PROPERTIES
    public string BrgId { get; init; }
    public string ReceiptSourceId { get; init; }
    public string LayananId { get; init; }
    #endregion

    #region BEHAVIOR
    public StockLedgerScopeKeyType ToLedgerScope()
        => StockLedgerScopeKeyType.Create(BrgId, ReceiptSourceId);
    #endregion
}

public interface IStockWriteScopeKey : IStockLedgerScopeKey, ILayananKey
{
}
