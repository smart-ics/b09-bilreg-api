namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

public static class StockLedgerSentinel
{
    /// <summary>
    /// Sentinel empty datetime (DATABASE / architecture §8). Used for absent Expiration Date.
    /// </summary>
    public static readonly DateTime EmptyDate = new(3000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
}
