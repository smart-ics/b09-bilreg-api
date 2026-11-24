namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// Parameterized reads of legacy <c>tb_stok</c> / <c>tb_buku</c> for one Item + Receipt Source.
/// Does not mutate legacy (UC-STL-010).
/// </summary>
public interface ILegacyStockReadPort
{
    IReadOnlyList<LegacyStockJournalReadModel> ListJournals(string brgId, string brgMasukReffId);

    IReadOnlyList<LegacyStockBalanceReadModel> ListBalances(string brgId, string brgMasukReffId);

    IReadOnlyList<string> ListActiveDoIdsAtLocation(string brgId, string layananId);

    IReadOnlyList<LegacyStockBalanceReadModel> ListBalancesAtLocation(string brgId, string layananId);

    IReadOnlyList<LegacyStockJournalReadModel> ListJournalsBySource(
        string trsReffId,
        string? brgId = null,
        string? layananId = null,
        string? movementKindString = null);
}

/// <summary>
/// Application-facing projection of one <c>tb_buku</c> row for hydrate/catch-up replay.
/// </summary>
public sealed record LegacyStockJournalReadModel(
    string LegacyBukuId,
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    string PoReffId,
    string NoBatch,
    decimal QtyIn,
    decimal QtyOut,
    decimal Hpp,
    string MovementKindString,
    string TrsReffId,
    DateTime TglMutasi,
    DateTime TglEd,
    string SatuanId = "");

/// <summary>
/// Application-facing projection of one <c>tb_stok</c> row for a scoped Item + DO.
/// </summary>
public sealed record LegacyStockBalanceReadModel(
    string LegacyStokId,
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    DateTime TglEd,
    string NoBatch,
    decimal QtySisa,
    decimal Hpp,
    DateTime TglMasuk,
    string PoReffId = "",
    string SatuanId = "");
