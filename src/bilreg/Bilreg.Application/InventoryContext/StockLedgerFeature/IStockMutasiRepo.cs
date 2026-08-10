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
    IEnumerable<StockMovementModel> ListByTrsReffId(string trsReffId);
}
