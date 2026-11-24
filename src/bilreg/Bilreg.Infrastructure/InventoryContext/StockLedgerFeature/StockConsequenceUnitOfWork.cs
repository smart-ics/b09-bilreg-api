using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// Single SQL transaction committing stock consequences exclusively to legacy <c>tb_stok</c> and <c>tb_buku</c>.
/// </summary>
public class StockConsequenceUnitOfWork : IStockConsequenceUnitOfWork
{
    private readonly ILegacyStockWriterPort _legacyWriter;

    /// <summary>
    /// Test hook: invoked after legacy writes so transaction rollback/atomicity can be asserted.
    /// </summary>
    internal Action? FailAfterLegacyWritesForTest { get; set; }

    public StockConsequenceUnitOfWork(ILegacyStockWriterPort legacyWriter)
    {
        _legacyWriter = legacyWriter;
    }

    public void Commit(StockConsequenceDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        using var scope = TransHelper.NewScope();

        foreach (var op in draft.LegacyOperations ?? [])
            DispatchLegacy(op);

        FailAfterLegacyWritesForTest?.Invoke();

        scope.Complete();
    }

    private void DispatchLegacy(LegacyStockWriteOperation op)
    {
        switch (op)
        {
            case LegacyInboundWriteOperation inbound:
                _legacyWriter.InsertInbound(inbound.Request);
                break;
            case LegacyOutboundBukuWriteOperation outbound:
                _legacyWriter.InsertOutboundBuku(outbound.Request);
                break;
            case LegacyStokDepleteWriteOperation deplete:
                _legacyWriter.DepleteStok(deplete.Request);
                break;
            case LegacyReverseBukuWriteOperation reverse:
                _legacyWriter.InsertReverseBuku(reverse.Request);
                break;
            case LegacyStokRestoreWriteOperation restore:
                _legacyWriter.RestoreStok(restore.Request);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported legacy write operation: {op.GetType().Name}.");
        }
    }
}
