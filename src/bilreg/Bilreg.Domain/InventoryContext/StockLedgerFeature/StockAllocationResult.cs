namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Outcome of outbound allocation. Insufficient stock returns partial lines plus explicit shortfall (BR-STL-027).
/// </summary>
public record StockAllocationResult
{
    private StockAllocationResult(
        bool isSuccess,
        decimal requestedQty,
        decimal allocatedQty,
        decimal shortfallQty,
        IReadOnlyList<StockAllocationLineType> lines)
    {
        IsSuccess = isSuccess;
        RequestedQty = requestedQty;
        AllocatedQty = allocatedQty;
        ShortfallQty = shortfallQty;
        Lines = lines;
    }

    public bool IsSuccess { get; }
    public decimal RequestedQty { get; }
    public decimal AllocatedQty { get; }
    public decimal ShortfallQty { get; }
    public IReadOnlyList<StockAllocationLineType> Lines { get; }

    public static StockAllocationResult Success(
        decimal requestedQty,
        IReadOnlyList<StockAllocationLineType> lines)
    {
        var allocated = lines.Sum(x => x.QtyAllocated);
        return new StockAllocationResult(
            isSuccess: true,
            requestedQty,
            allocated,
            shortfallQty: 0,
            lines);
    }

    public static StockAllocationResult Insufficient(
        decimal requestedQty,
        IReadOnlyList<StockAllocationLineType> lines)
    {
        var allocated = lines.Sum(x => x.QtyAllocated);
        return new StockAllocationResult(
            isSuccess: false,
            requestedQty,
            allocated,
            shortfallQty: requestedQty - allocated,
            lines);
    }
}
