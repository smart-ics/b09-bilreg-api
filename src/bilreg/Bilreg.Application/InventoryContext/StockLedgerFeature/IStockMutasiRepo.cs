using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Insert-only Stock Movement journal. No Update/Delete for quantity/kind (ADR-STL-002).
/// </summary>
public interface IStockMutasiRepo
{
    void Insert(StockMovementModel model);
    void Insert(StockMovementModel model, string userId);
    bool Exists(string trsReffId, MovementKindEnum kind, string stokLokasiId);
    bool ExistsReversalFor(string originalStokMutasiId);
    IEnumerable<StockMovementModel> ListByTrsReffId(string trsReffId);

    /// <summary>
    /// All movements for Item + Receipt Source (± optional Stock Location). Used by UC-STL-020.
    /// </summary>
    IEnumerable<StockMovementModel> ListByScope(string brgId, string brgMasukReffId, string? layananId = null);
}
