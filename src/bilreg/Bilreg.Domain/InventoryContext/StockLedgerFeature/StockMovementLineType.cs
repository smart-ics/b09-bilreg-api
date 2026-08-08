using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// One inbound or outbound inventory effect within a Stock Movement.
/// Completed lines are immutable facts retained for provenance.
/// </summary>
public record StockMovementLineType
{
    #region CREATION
    public StockMovementLineType(
        int lineNo,
        string brgId,
        string receiptSourceId,
        string layananId,
        StockMovementDirectionEnum direction,
        decimal quantity,
        UnitValuationType unitValuation,
        StockFactOriginEnum origin,
        string? stockLayerId = null)
    {
        Guard.Against.NegativeOrZero(lineNo, nameof(lineNo));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.Null(unitValuation, nameof(unitValuation));
        Guard.Against.EnumOutOfRange(direction, nameof(direction));
        Guard.Against.EnumOutOfRange(origin, nameof(origin));

        LineNo = lineNo;
        BrgId = brgId;
        ReceiptSourceId = receiptSourceId;
        LayananId = layananId;
        Direction = direction;
        Quantity = quantity;
        UnitValuation = unitValuation;
        Origin = origin;
        StockLayerId = string.IsNullOrWhiteSpace(stockLayerId) ? null : stockLayerId;
    }

    public static StockMovementLineType Create(
        int lineNo,
        IBrgKey item,
        IReceiptSourceKey receiptSource,
        ILayananKey stockLocation,
        StockMovementDirectionEnum direction,
        decimal quantity,
        UnitValuationType unitValuation,
        StockFactOriginEnum origin,
        IStockLayerKey? stockLayer = null)
    {
        Guard.Against.Null(item, nameof(item));
        Guard.Against.Null(receiptSource, nameof(receiptSource));
        Guard.Against.Null(stockLocation, nameof(stockLocation));

        return new StockMovementLineType(
            lineNo,
            item.BrgId,
            receiptSource.ReceiptSourceId,
            stockLocation.LayananId,
            direction,
            quantity,
            unitValuation,
            origin,
            stockLayer?.StockLayerId);
    }
    #endregion

    #region PROPERTIES
    public int LineNo { get; init; }
    public string BrgId { get; init; }
    public string ReceiptSourceId { get; init; }
    public string LayananId { get; init; }
    public StockMovementDirectionEnum Direction { get; init; }
    public decimal Quantity { get; init; }
    public UnitValuationType UnitValuation { get; init; }
    /// <summary>Origin only — never authority.</summary>
    public StockFactOriginEnum Origin { get; init; }
    /// <summary>
    /// Consumed/established Stock Layer when known (BR-STL-024/025). Optional for inbound
    /// lines that establish a layer only after persistence assigns identity.
    /// </summary>
    public string? StockLayerId { get; init; }
    #endregion

    #region BEHAVIOR
    public StockMovementLineType WithOppositeDirection(int lineNo)
        => this with
        {
            LineNo = lineNo,
            Direction = Direction == StockMovementDirectionEnum.Inbound
                ? StockMovementDirectionEnum.Outbound
                : StockMovementDirectionEnum.Inbound
        };

    public StockMovementLineType WithStockLayer(IStockLayerKey stockLayer)
    {
        Guard.Against.Null(stockLayer, nameof(stockLayer));
        Guard.Against.NullOrWhiteSpace(stockLayer.StockLayerId, nameof(stockLayer));
        return this with { StockLayerId = stockLayer.StockLayerId };
    }
    #endregion
}
