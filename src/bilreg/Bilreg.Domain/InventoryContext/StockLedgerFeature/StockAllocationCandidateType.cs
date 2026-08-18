namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Immutable allocation candidate for outbound selection (BR-STL-022…027).
/// <see cref="NoBatch"/> is intentionally omitted (GAP-STL-005).
/// </summary>
public record StockAllocationCandidateType(
    string BrgId,
    string LayananId,
    string StokLokasiId,
    string StokBatchId,
    string BrgMasukReffId,
    DateTime TglEd,
    DateTime TglMasuk,
    decimal QtySisa)
{
    public static StockAllocationCandidateType FromLokasi(LocationStockBalanceModel lokasi) =>
        new(
            lokasi.BrgId,
            lokasi.LayananId,
            lokasi.StokLokasiId,
            lokasi.StokBatchId,
            lokasi.BrgMasukReffId,
            lokasi.TglEd,
            lokasi.TglMasuk,
            lokasi.QtySisa);
}
