namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// How a Stock Ledger fact/layer was established.
/// Origin only — never authority. Runtime authority during coexistence remains tb_stok + tb_buku.
/// </summary>
public enum StockFactOriginEnum
{
    Native = 1,
    Reconstructed = 2,
    LegacySynchronized = 3
}
