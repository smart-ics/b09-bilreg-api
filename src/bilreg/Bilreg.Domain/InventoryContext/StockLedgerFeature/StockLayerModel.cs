using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Accountable quantity layer at one Stock Location (BR-STL-013–018).
/// Depleted layers remain retained with Remaining Quantity = 0.
/// </summary>
public record StockLayerModel : IStockLayerKey
{
    #region CREATION
    private StockLayerModel(
        string stockLayerId,
        string brgId,
        string receiptSourceId,
        string layananId,
        string layerFormingMovementId,
        decimal initialQuantity,
        decimal remainingQuantity,
        UnitValuationType unitValuation,
        DateOnly? expirationDate,
        DateTime effectiveReceiptTime,
        StockFactOriginEnum origin,
        string? batch)
    {
        Guard.Against.NullOrWhiteSpace(stockLayerId, nameof(stockLayerId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(layerFormingMovementId, nameof(layerFormingMovementId));
        Guard.Against.NegativeOrZero(initialQuantity, nameof(initialQuantity));
        Guard.Against.Negative(remainingQuantity, nameof(remainingQuantity));
        Guard.Against.Null(unitValuation, nameof(unitValuation));
        Guard.Against.Default(effectiveReceiptTime, nameof(effectiveReceiptTime));
        Guard.Against.EnumOutOfRange(origin, nameof(origin));

        if (remainingQuantity > initialQuantity)
            throw new ArgumentException(
                "Remaining Quantity must not exceed Initial Quantity.",
                nameof(remainingQuantity));

        StockLayerId = stockLayerId;
        BrgId = brgId;
        ReceiptSourceId = receiptSourceId;
        LayananId = layananId;
        LayerFormingMovementId = layerFormingMovementId;
        InitialQuantity = initialQuantity;
        RemainingQuantity = remainingQuantity;
        UnitValuation = unitValuation;
        ExpirationDate = expirationDate;
        EffectiveReceiptTime = effectiveReceiptTime;
        Origin = origin;
        Batch = batch;
    }

    /// <summary>
    /// Establishes a Stock Layer with positive Initial Quantity (BR-STL-014).
    /// When <paramref name="remainingQuantity"/> is omitted, Remaining Quantity starts equal to Initial Quantity.
    /// Callers may supply Remaining Quantity ≤ Initial (including zero) to establish depleted or
    /// partially consumed reconstructed layers without inventing unavailable historical identities
    /// (BR-STL-065–068).
    /// </summary>
    public static StockLayerModel Create(
        IBrgKey item,
        IReceiptSourceKey receiptSource,
        ILayananKey stockLocation,
        IStockMovementKey layerFormingMovement,
        decimal initialQuantity,
        UnitValuationType unitValuation,
        DateTime effectiveReceiptTime,
        StockFactOriginEnum origin,
        DateOnly? expirationDate = null,
        string? batch = null,
        string? stockLayerId = null,
        decimal? remainingQuantity = null)
    {
        Guard.Against.Null(item, nameof(item));
        Guard.Against.Null(receiptSource, nameof(receiptSource));
        Guard.Against.Null(stockLocation, nameof(stockLocation));
        Guard.Against.Null(layerFormingMovement, nameof(layerFormingMovement));
        Guard.Against.NegativeOrZero(initialQuantity, nameof(initialQuantity));

        var remaining = remainingQuantity ?? initialQuantity;

        return new StockLayerModel(
            string.IsNullOrWhiteSpace(stockLayerId)
                ? Ulid.NewUlid().ToString()
                : stockLayerId,
            item.BrgId,
            receiptSource.ReceiptSourceId,
            stockLocation.LayananId,
            layerFormingMovement.StockMovementId,
            initialQuantity,
            remaining,
            unitValuation,
            expirationDate,
            effectiveReceiptTime,
            origin,
            batch);
    }

    public static IStockLayerKey Key(string stockLayerId)
        => new StockLayerKey(stockLayerId);

    /// <summary>
    /// Rehydrates a Stock Layer from durable storage, including depleted layers
    /// (Remaining Quantity = 0). Does not invent identity or mutate existing facts.
    /// </summary>
    public static StockLayerModel Rehydrate(
        string stockLayerId,
        string brgId,
        string receiptSourceId,
        string layananId,
        string layerFormingMovementId,
        decimal initialQuantity,
        decimal remainingQuantity,
        UnitValuationType unitValuation,
        DateTime effectiveReceiptTime,
        StockFactOriginEnum origin,
        DateOnly? expirationDate = null,
        string? batch = null)
        => new(
            stockLayerId,
            brgId,
            receiptSourceId,
            layananId,
            layerFormingMovementId,
            initialQuantity,
            remainingQuantity,
            unitValuation,
            expirationDate,
            effectiveReceiptTime,
            origin,
            batch);
    #endregion

    #region PROPERTIES
    public string StockLayerId { get; init; }
    public string BrgId { get; init; }
    public string ReceiptSourceId { get; init; }
    public string LayananId { get; init; }
    public string LayerFormingMovementId { get; init; }
    public decimal InitialQuantity { get; init; }
    public decimal RemainingQuantity { get; init; }
    public UnitValuationType UnitValuation { get; init; }
    public DateOnly? ExpirationDate { get; init; }
    public DateTime EffectiveReceiptTime { get; init; }
    /// <summary>Origin only — never authority.</summary>
    public StockFactOriginEnum Origin { get; init; }
    /// <summary>
    /// Optional informational batch label. Never used for allocation selection or ordering.
    /// </summary>
    public string? Batch { get; init; }
    public bool IsDepleted => RemainingQuantity == 0m;
    public bool HasAvailableQuantity => RemainingQuantity > 0m;
    #endregion

    #region BEHAVIOR
    /// <summary>
    /// Returns a new layer with reduced Remaining Quantity.
    /// Depleted layers are retained at zero (BR-STL-016–018).
    /// </summary>
    public StockLayerModel Consume(decimal quantity)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        if (quantity > RemainingQuantity)
            throw new ArgumentException(
                $"Cannot consume {quantity}; Remaining Quantity is {RemainingQuantity}.",
                nameof(quantity));

        return this with { RemainingQuantity = RemainingQuantity - quantity };
    }

    public StockWriteScopeKeyType ToWriteScope()
        => StockWriteScopeKeyType.Create(BrgId, ReceiptSourceId, LayananId);
    #endregion

    private sealed record StockLayerKey(string StockLayerId) : IStockLayerKey;
}

public interface IStockLayerKey
{
    string StockLayerId { get; }
}
