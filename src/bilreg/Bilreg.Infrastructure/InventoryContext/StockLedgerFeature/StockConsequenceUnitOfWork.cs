using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// Atomic dual-write: legacy → BILRG batch/lokasi (OCC) → mutasi → binding → scope.
/// When <see cref="StockLedgerCoexistenceOptions.CoexistenceEnabled"/> is false, skips legacy writes (ADR-STL-007).
/// </summary>
public class StockConsequenceUnitOfWork : IStockConsequenceUnitOfWork
{
    private readonly ILegacyStockWriterPort _legacyWriter;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IStockMutasiRepo _mutasiRepo;
    private readonly IStockLegacyBindingRepo _bindingRepo;
    private readonly IStockLegacyScopeRepo _scopeRepo;
    private readonly StockLedgerCoexistenceOptions _coexistence;

    /// <summary>
    /// Test hook: invoked after legacy writes and before ledger persist so atomicity can be asserted.
    /// </summary>
    internal Action? FailAfterLegacyWritesForTest { get; set; }

    public StockConsequenceUnitOfWork(
        ILegacyStockWriterPort legacyWriter,
        IStockBatchRepo batchRepo,
        IStockMutasiRepo mutasiRepo,
        IStockLegacyBindingRepo bindingRepo,
        IStockLegacyScopeRepo scopeRepo,
        IOptions<StockLedgerCoexistenceOptions> coexistence)
    {
        _legacyWriter = legacyWriter;
        _batchRepo = batchRepo;
        _mutasiRepo = mutasiRepo;
        _bindingRepo = bindingRepo;
        _scopeRepo = scopeRepo;
        _coexistence = coexistence.Value;
    }

    public void Commit(StockConsequenceDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        using var scope = TransHelper.NewScope();

        if (_coexistence.CoexistenceEnabled)
        {
            foreach (var op in draft.LegacyOperations ?? [])
                DispatchLegacy(op);

            FailAfterLegacyWritesForTest?.Invoke();
        }

        foreach (var batch in draft.BatchUpserts ?? [])
            _batchRepo.SaveChanges(batch, draft.UserId);

        foreach (var mutasi in draft.MutasiInserts ?? [])
            _mutasiRepo.Insert(mutasi, draft.UserId);

        foreach (var binding in draft.BindingInserts ?? [])
            _bindingRepo.Insert(binding, draft.UserId);

        foreach (var scopeUpdate in draft.ScopeUpdates ?? [])
            _scopeRepo.SaveChanges(scopeUpdate, draft.UserId);

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
            default:
                throw new InvalidOperationException(
                    $"Unsupported legacy write operation: {op.GetType().Name}.");
        }
    }
}
