using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Write-consistency Stock Position: Item + Receipt Source + Stock Location.
/// Distinct from reconstruction scope (Item + Receipt Source). Does not encode authority.
/// Retains depleted layers (BR-STL-017).
/// </summary>
public record StockPositionModel : IStockWriteScopeKey
{
    private readonly IReadOnlyList<StockLayerModel> _layers;

    #region CREATION
    private StockPositionModel(
        string brgId,
        string receiptSourceId,
        string layananId,
        long version,
        IReadOnlyList<StockLayerModel> layers)
    {
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.Negative(version, nameof(version));
        Guard.Against.Null(layers, nameof(layers));

        BrgId = brgId;
        ReceiptSourceId = receiptSourceId;
        LayananId = layananId;
        Version = version;
        _layers = layers;
    }

    public static StockPositionModel CreateEmpty(
        IBrgKey item,
        IReceiptSourceKey receiptSource,
        ILayananKey stockLocation)
    {
        Guard.Against.Null(item, nameof(item));
        Guard.Against.Null(receiptSource, nameof(receiptSource));
        Guard.Against.Null(stockLocation, nameof(stockLocation));

        return new StockPositionModel(
            item.BrgId,
            receiptSource.ReceiptSourceId,
            stockLocation.LayananId,
            version: 0,
            Array.AsReadOnly(Array.Empty<StockLayerModel>()));
    }

    public static StockPositionModel CreateEmpty(IStockWriteScopeKey writeScope)
    {
        Guard.Against.Null(writeScope, nameof(writeScope));
        return new StockPositionModel(
            writeScope.BrgId,
            writeScope.ReceiptSourceId,
            writeScope.LayananId,
            version: 0,
            Array.AsReadOnly(Array.Empty<StockLayerModel>()));
    }

    public static StockPositionModel Create(
        IStockWriteScopeKey writeScope,
        IEnumerable<StockLayerModel> layers,
        long version = 0)
    {
        Guard.Against.Null(writeScope, nameof(writeScope));
        var frozen = FreezeLayers(layers, writeScope);
        return new StockPositionModel(
            writeScope.BrgId,
            writeScope.ReceiptSourceId,
            writeScope.LayananId,
            version,
            frozen);
    }
    #endregion

    #region PROPERTIES
    public string BrgId { get; init; }
    public string ReceiptSourceId { get; init; }
    public string LayananId { get; init; }
    /// <summary>Optimistic concurrency token (domain portion of G-06).</summary>
    public long Version { get; init; }
    public IReadOnlyList<StockLayerModel> Layers => _layers;
    public decimal TotalRemainingQuantity => _layers.Sum(x => x.RemainingQuantity);
    public StockWriteScopeKeyType WriteScope
        => StockWriteScopeKeyType.Create(BrgId, ReceiptSourceId, LayananId);
    public StockLedgerScopeKeyType LedgerScope
        => StockLedgerScopeKeyType.Create(BrgId, ReceiptSourceId);
    #endregion

    #region BEHAVIOR
    /// <summary>
    /// Adds a layer belonging to this write scope. Depleted layers already present stay retained.
    /// </summary>
    public StockPositionModel AddLayer(StockLayerModel layer)
    {
        Guard.Against.Null(layer, nameof(layer));
        EnsureLayerBelongs(layer);

        if (_layers.Any(x => x.StockLayerId == layer.StockLayerId))
            throw new ArgumentException(
                $"Stock Layer '{layer.StockLayerId}' already exists in this position.",
                nameof(layer));

        var next = _layers.Concat([layer]).ToArray();
        return new StockPositionModel(
            BrgId,
            ReceiptSourceId,
            LayananId,
            Version + 1,
            Array.AsReadOnly(next));
    }

    /// <summary>
    /// Allocates from this position using ED-constrained FIFO.
    /// On success returns a new position with consumed (including zero) layers retained.
    /// On insufficient stock returns the original position unchanged.
    /// </summary>
    public (StockAllocationResult Result, StockPositionModel Position) Allocate(
        decimal quantity,
        DateOnly? expirationDate = null)
    {
        var result = StockFifoAllocator.Allocate(
            _layers,
            BrgObatType.Key(BrgId),
            LayananType.Key(LayananId),
            quantity,
            expirationDate);

        if (!result.IsFulfilled)
            return (result, this);

        var next = new StockPositionModel(
            BrgId,
            ReceiptSourceId,
            LayananId,
            Version + 1,
            result.UpdatedLayers);

        return (result, next);
    }
    #endregion

    #region INVARIANTS
    private static IReadOnlyList<StockLayerModel> FreezeLayers(
        IEnumerable<StockLayerModel> layers,
        IStockWriteScopeKey writeScope)
    {
        Guard.Against.Null(layers, nameof(layers));
        var list = layers.ToArray();
        var ids = list.Select(x => x.StockLayerId).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new ArgumentException("Stock Layer identities must be unique within a position.", nameof(layers));

        foreach (var layer in list)
        {
            if (layer.BrgId != writeScope.BrgId
                || layer.ReceiptSourceId != writeScope.ReceiptSourceId
                || layer.LayananId != writeScope.LayananId)
            {
                throw new ArgumentException(
                    "All Stock Layers must match the position write scope (Item + Receipt Source + Stock Location).",
                    nameof(layers));
            }
        }

        return Array.AsReadOnly(list);
    }

    private void EnsureLayerBelongs(StockLayerModel layer)
    {
        if (layer.BrgId != BrgId
            || layer.ReceiptSourceId != ReceiptSourceId
            || layer.LayananId != LayananId)
        {
            throw new ArgumentException(
                "Stock Layer write scope must match the Stock Position.",
                nameof(layer));
        }
    }
    #endregion
}
