using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// One accountable quantity taken from a single Stock Layer during FIFO allocation
/// (BR-STL-034, BR-STL-035).
/// </summary>
public record StockLayerAllocationType
{
    #region CREATION
    public StockLayerAllocationType(
        string stockLayerId,
        string brgId,
        string receiptSourceId,
        string layananId,
        decimal quantity,
        UnitValuationType unitValuation,
        DateOnly? expirationDate,
        StockFactOriginEnum origin)
    {
        Guard.Against.NullOrWhiteSpace(stockLayerId, nameof(stockLayerId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(receiptSourceId, nameof(receiptSourceId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.Null(unitValuation, nameof(unitValuation));
        Guard.Against.EnumOutOfRange(origin, nameof(origin));

        StockLayerId = stockLayerId;
        BrgId = brgId;
        ReceiptSourceId = receiptSourceId;
        LayananId = layananId;
        Quantity = quantity;
        UnitValuation = unitValuation;
        ExpirationDate = expirationDate;
        Origin = origin;
    }
    #endregion

    #region PROPERTIES
    public string StockLayerId { get; init; }
    public string BrgId { get; init; }
    public string ReceiptSourceId { get; init; }
    public string LayananId { get; init; }
    public decimal Quantity { get; init; }
    public UnitValuationType UnitValuation { get; init; }
    public DateOnly? ExpirationDate { get; init; }
    /// <summary>Origin only — never authority.</summary>
    public StockFactOriginEnum Origin { get; init; }
    #endregion

    #region BEHAVIOR
    public StockMovementLineType ToOutboundMovementLine(int lineNo)
        => new(
            lineNo,
            BrgId,
            ReceiptSourceId,
            LayananId,
            StockMovementDirectionEnum.Outbound,
            Quantity,
            UnitValuation,
            Origin,
            StockLayerId);
    #endregion
}
