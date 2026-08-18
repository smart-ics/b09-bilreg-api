namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// One outbound allocation slice against a single Location Stock Balance.
/// </summary>
public record StockAllocationLineType(
    string StokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    DateTime TglEd,
    DateTime TglMasuk,
    decimal QtyAllocated);
