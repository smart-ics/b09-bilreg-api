namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Reconstruction readiness for an Item + Receipt Source scope (across all Stock Locations).
/// Not an authority or ownership state.
/// </summary>
public enum ReconstructionStatusEnum
{
    NotReconstructed = 1,
    ReconstructionRequired = 2,
    Reconstructing = 3,
    Reconstructed = 4,
    Inconsistent = 5
}
