using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Draft payload for atomic ledger + legacy + binding commit (implemented in S1-D1).
/// </summary>
public record StockConsequenceDraft(
    IReadOnlyList<StockBatchModel> BatchUpserts,
    IReadOnlyList<StockMovementModel> MutasiInserts,
    IReadOnlyList<StockLegacyBindingModel> BindingInserts,
    StockLegacyScopeModel? ScopeUpdate,
    string UserId);

/// <summary>
/// Contract shell for dual-write UoW. Body lands in S1-D1.
/// </summary>
public interface IStockConsequenceUnitOfWork
{
    void Commit(StockConsequenceDraft draft);
}
