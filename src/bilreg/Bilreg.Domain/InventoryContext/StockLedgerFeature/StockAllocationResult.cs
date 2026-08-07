using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Explicit FIFO allocation outcome. Insufficient stock yields an unfulfilled result
/// without mutating layers (BR-STL-036, BR-STL-037).
/// </summary>
public record StockAllocationResult
{
    #region CREATION
    private StockAllocationResult(
        decimal requestedQuantity,
        decimal allocatedQuantity,
        IReadOnlyList<StockLayerAllocationType> allocations,
        IReadOnlyList<StockLayerModel> updatedLayers)
    {
        Guard.Against.NegativeOrZero(requestedQuantity, nameof(requestedQuantity));
        Guard.Against.Negative(allocatedQuantity, nameof(allocatedQuantity));
        Guard.Against.Null(allocations, nameof(allocations));
        Guard.Against.Null(updatedLayers, nameof(updatedLayers));

        RequestedQuantity = requestedQuantity;
        AllocatedQuantity = allocatedQuantity;
        Allocations = allocations;
        UpdatedLayers = updatedLayers;
    }

    public static StockAllocationResult Fulfilled(
        decimal requestedQuantity,
        IReadOnlyList<StockLayerAllocationType> allocations,
        IReadOnlyList<StockLayerModel> updatedLayers)
    {
        Guard.Against.Null(allocations, nameof(allocations));
        if (allocations.Count == 0)
            throw new ArgumentException("Fulfilled allocation must identify at least one layer.", nameof(allocations));

        var allocated = allocations.Sum(x => x.Quantity);
        if (allocated != requestedQuantity)
            throw new ArgumentException(
                $"Fulfilled allocation quantity {allocated} must equal requested {requestedQuantity}.",
                nameof(allocations));

        return new StockAllocationResult(
            requestedQuantity,
            allocated,
            Array.AsReadOnly(allocations.ToArray()),
            Array.AsReadOnly(updatedLayers.ToArray()));
    }

    public static StockAllocationResult Insufficient(
        decimal requestedQuantity,
        decimal availableQuantity)
    {
        Guard.Against.NegativeOrZero(requestedQuantity, nameof(requestedQuantity));
        Guard.Against.Negative(availableQuantity, nameof(availableQuantity));
        if (availableQuantity >= requestedQuantity)
            throw new ArgumentException(
                "Insufficient result requires available quantity below requested quantity.",
                nameof(availableQuantity));

        return new StockAllocationResult(
            requestedQuantity,
            allocatedQuantity: 0m,
            Array.AsReadOnly(Array.Empty<StockLayerAllocationType>()),
            Array.AsReadOnly(Array.Empty<StockLayerModel>()));
    }
    #endregion

    #region PROPERTIES
    public decimal RequestedQuantity { get; init; }
    public decimal AllocatedQuantity { get; init; }
    public decimal UnfulfilledQuantity => RequestedQuantity - AllocatedQuantity;
    public bool IsFulfilled => UnfulfilledQuantity == 0m && Allocations.Count > 0;
    public IReadOnlyList<StockLayerAllocationType> Allocations { get; init; }
    /// <summary>
    /// Full layer set after consumption when fulfilled; empty when insufficient
    /// (caller must keep the original layers).
    /// </summary>
    public IReadOnlyList<StockLayerModel> UpdatedLayers { get; init; }
    #endregion

    #region BEHAVIOR
    public IReadOnlyList<StockMovementLineType> ToOutboundMovementLines(int startLineNo = 1)
    {
        if (!IsFulfilled)
            throw new InvalidOperationException(
                "Cannot build outbound movement lines from an unfulfilled allocation.");

        Guard.Against.NegativeOrZero(startLineNo, nameof(startLineNo));
        return Allocations
            .Select((allocation, index) => allocation.ToOutboundMovementLine(startLineNo + index))
            .ToArray();
    }
    #endregion
}
