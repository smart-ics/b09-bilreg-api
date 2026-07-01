namespace Farinv.Domain.InventoryContext.MutasiFeature;

public enum OrderMutasiStateEnum
{
    Draft,       // dikumpulkan dari request per barang
    Submitted,   // siap direview
    Approved,    // disetujui
    Rejected,    // ditolak
    Completed    // mutasi sudah dieksekusi
}
