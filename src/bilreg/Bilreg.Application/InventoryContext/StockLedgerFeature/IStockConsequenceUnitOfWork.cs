using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Draft payload for atomic ledger + legacy + binding commit (S1-D1).
/// </summary>
public record StockConsequenceDraft(
    IReadOnlyList<StockBatchModel> BatchUpserts,
    IReadOnlyList<StockMovementModel> MutasiInserts,
    IReadOnlyList<StockLegacyBindingModel> BindingInserts,
    IReadOnlyList<StockLegacyScopeModel> ScopeUpdates,
    IReadOnlyList<LegacyStockWriteOperation> LegacyOperations,
    string UserId);

/// <summary>
/// Single SQL transaction spanning ledger + legacy + scope/binding (architecture §13).
/// </summary>
public interface IStockConsequenceUnitOfWork
{
    void Commit(StockConsequenceDraft draft);
}
