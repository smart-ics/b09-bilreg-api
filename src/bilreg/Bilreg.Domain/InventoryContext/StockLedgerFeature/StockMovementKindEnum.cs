namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Business movement kind for an accountable Stock Movement.
/// Foundation set for Phase 1 — not every legacy transaction type.
/// </summary>
public enum StockMovementKindEnum
{
    Receipt = 1,
    Outbound = 2,
    Transfer = 3,
    Correction = 4,
    Reversal = 5
}
