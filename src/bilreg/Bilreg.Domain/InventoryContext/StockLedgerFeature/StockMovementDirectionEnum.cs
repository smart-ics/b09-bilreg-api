namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Explicit inventory direction. Quantity is always positive;
/// direction is never encoded as a negative quantity (BR-STL-023).
/// </summary>
public enum StockMovementDirectionEnum
{
    Inbound = 1,
    Outbound = 2
}
