using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.Shared;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// Coordinates Stock Ledger consequence persistence through existing repos
/// inside the ambient <see cref="IUnitOfWork"/> boundary (<c>TransHelper</c>).
/// Does not open raw SQL connections or write <c>tb_stok</c> / <c>tb_buku</c>.
/// </summary>
public sealed class StockConsequenceUnitOfWork : IStockConsequenceUnitOfWork
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;
    private readonly IStockMovementRepo _movementRepo;
    private readonly IStockPositionRepo _positionRepo;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;
    private readonly ILegacyCompatibilityWriterPort _legacyCompatibilityWriter;

    public StockConsequenceUnitOfWork(
        IUnitOfWork unitOfWork,
        IStockSourceIdempotencyRepo idempotencyRepo,
        IStockMovementRepo movementRepo,
        IStockPositionRepo positionRepo,
        IStockLedgerScopeStateRepo scopeStateRepo,
        ILegacyCompatibilityWriterPort legacyCompatibilityWriter)
    {
        _unitOfWork = unitOfWork;
        _idempotencyRepo = idempotencyRepo;
        _movementRepo = movementRepo;
        _positionRepo = positionRepo;
        _scopeStateRepo = scopeStateRepo;
        _legacyCompatibilityWriter = legacyCompatibilityWriter;
    }

    public StockConsequenceCommitResult Commit(StockConsequenceDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        Guard.Against.NullOrWhiteSpace(draft.IdempotencyKey, nameof(draft.IdempotencyKey));
        Guard.Against.Default(draft.ProcessedAt, nameof(draft.ProcessedAt));
        Guard.Against.Null(draft.Movement, nameof(draft.Movement));
        Guard.Against.Null(draft.Positions, nameof(draft.Positions));
        Guard.Against.EnumOutOfRange(draft.IdempotencyKind, nameof(draft.IdempotencyKind));

        using var tx = _unitOfWork.Begin();

        var idempotency = BuildIdempotency(draft);
        var insert = _idempotencyRepo.InsertOrGetExisting(idempotency);
        if (!insert.WasInserted)
        {
            // Prior successful consequence already owns this key — do not rewrite or re-call legacy.
            // Dispose without Complete so this attempt leaves no new durable rows.
            return new StockConsequenceCommitResult(
                StockConsequenceCommitOutcomeEnum.AlreadyCommitted,
                draft.IdempotencyKey.Trim(),
                insert.Record.StockMovementId,
                insert.Record.IdempotencyId);
        }

        _movementRepo.SaveChanges(draft.Movement);

        foreach (var position in draft.Positions)
            _positionRepo.SaveChanges(position);

        if (draft.ScopeState is not null)
            PersistScope(draft.ScopeState, draft.ExpectedPriorReconstructionStatus);

        if (draft.LegacyWrite is not null)
            _legacyCompatibilityWriter.Apply(draft.LegacyWrite);

        tx.Complete();

        return new StockConsequenceCommitResult(
            StockConsequenceCommitOutcomeEnum.Committed,
            draft.IdempotencyKey.Trim(),
            draft.Movement.StockMovementId,
            insert.Record.IdempotencyId);
    }

    private void PersistScope(
        StockLedgerScopeStateModel scopeState,
        ReconstructionStatusEnum? expectedPriorReconstructionStatus)
    {
        if (expectedPriorReconstructionStatus is null)
        {
            _scopeStateRepo.SaveChanges(scopeState);
            return;
        }

        if (!_scopeStateRepo.TryUpdateWhenReconstructionStatus(
                scopeState,
                expectedPriorReconstructionStatus.Value))
        {
            throw StockLedgerPersistenceException.Concurrency(
                $"Scope ({scopeState.BrgId}/{scopeState.ReceiptSourceId}) reconstruction " +
                $"status was no longer '{expectedPriorReconstructionStatus.Value}' at Phase C persist.");
        }
    }

    private static StockSourceIdempotencyModel BuildIdempotency(StockConsequenceDraft draft)
    {
        var primaryLine = draft.Movement.Lines[0];
        var brgId = draft.ScopeState?.BrgId ?? primaryLine.BrgId;
        var receiptSourceId = draft.ScopeState?.ReceiptSourceId ?? primaryLine.ReceiptSourceId;

        return StockSourceIdempotencyModel.Create(
            draft.IdempotencyKind,
            draft.IdempotencyKey.Trim(),
            draft.ProcessedAt,
            sourceTransactionId: draft.Movement.SourceTransactionId,
            stockMovementId: draft.Movement.StockMovementId,
            brgId: brgId,
            receiptSourceId: receiptSourceId);
    }
}
