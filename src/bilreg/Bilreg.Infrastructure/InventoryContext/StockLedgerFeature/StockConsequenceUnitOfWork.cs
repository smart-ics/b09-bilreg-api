using Bilreg.Application.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// Contract shell only. Real dual-write commit lands in S1-D1. Not registered in DI yet.
/// </summary>
public class StockConsequenceUnitOfWork : IStockConsequenceUnitOfWork
{
    public void Commit(StockConsequenceDraft draft) =>
        throw new NotImplementedException(
            "StockConsequenceUnitOfWork.Commit is implemented in S1-D1 (legacy writer + atomic TX).");
}
