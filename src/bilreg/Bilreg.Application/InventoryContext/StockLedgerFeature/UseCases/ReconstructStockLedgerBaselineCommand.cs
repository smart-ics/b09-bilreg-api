using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.Shared;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// P2-S6 — End-to-end Initial Reconstruction for one Item + Receipt Source (G-10 core).
/// Phase A claim (short TX) → Phase B read/fingerprint/calculate (no write TX) →
/// Phase C revalidate + persist or mark Inconsistent (short TX).
/// Does not transfer stock authority or modify legacy rows.
/// </summary>
public sealed record ReconstructStockLedgerBaselineCommand(
    string BrgId,
    string ReceiptSourceId) : IRequest<ReconstructStockLedgerBaselineResult>, IStockLedgerScopeKey;

public enum ReconstructStockLedgerBaselineOutcomeEnum
{
    /// <summary>Balanced baseline persisted; Scope is Reconstructed with sync position.</summary>
    Reconstructed = 1,

    /// <summary>Baseline could not be formed safely; Scope is Inconsistent with reason.</summary>
    Inconsistent = 2,

    /// <summary>Scope was already Reconstructed; no duplicate quantities/layers written.</summary>
    AlreadyReconstructed = 3,

    /// <summary>Scope was already Inconsistent; no overwrite without recovery.</summary>
    AlreadyInconsistent = 4,

    /// <summary>
    /// Legacy basis changed during Phase B and could not be settled within bounded retries.
    /// Scope remains Reconstructing (recoverable); no stale baseline persisted.
    /// </summary>
    BasisChangedRetryRequired = 5
}

public sealed record ReconstructStockLedgerBaselineResult(
    ReconstructStockLedgerBaselineOutcomeEnum Outcome,
    StockLedgerScopeStateModel ScopeState,
    string? InconsistencyReason,
    SynchronizationPositionType? SynchronizationPosition)
{
    public static ReconstructStockLedgerBaselineResult Reconstructed(StockLedgerScopeStateModel scope)
        => new(
            ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed,
            scope,
            InconsistencyReason: null,
            SynchronizationPosition: scope.SynchronizationPosition);

    public static ReconstructStockLedgerBaselineResult Inconsistent(StockLedgerScopeStateModel scope)
        => new(
            ReconstructStockLedgerBaselineOutcomeEnum.Inconsistent,
            scope,
            InconsistencyReason: scope.InconsistencyReason,
            SynchronizationPosition: null);

    public static ReconstructStockLedgerBaselineResult AlreadyReconstructed(StockLedgerScopeStateModel scope)
        => new(
            ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed,
            scope,
            InconsistencyReason: null,
            SynchronizationPosition: scope.SynchronizationPosition);

    public static ReconstructStockLedgerBaselineResult AlreadyInconsistent(StockLedgerScopeStateModel scope)
        => new(
            ReconstructStockLedgerBaselineOutcomeEnum.AlreadyInconsistent,
            scope,
            InconsistencyReason: scope.InconsistencyReason,
            SynchronizationPosition: null);

    public static ReconstructStockLedgerBaselineResult BasisChangedRetryRequired(
        StockLedgerScopeStateModel scope)
        => new(
            ReconstructStockLedgerBaselineOutcomeEnum.BasisChangedRetryRequired,
            scope,
            InconsistencyReason: null,
            SynchronizationPosition: null);
}

public sealed class ReconstructStockLedgerBaselineHandler
    : IRequestHandler<ReconstructStockLedgerBaselineCommand, ReconstructStockLedgerBaselineResult>
{
    /// <summary>
    /// Bounded Phase B→C retries when legacy basis moves during calculation.
    /// Leaves Scope Reconstructing when exhausted (recoverable; no stale persist).
    /// </summary>
    public const int MaxBasisChangeRetries = 3;

    private readonly ReconstructionClaimService _claimService;
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly IStockConsequenceUnitOfWork _consequenceUnitOfWork;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;

    public ReconstructStockLedgerBaselineHandler(
        ReconstructionClaimService claimService,
        ILegacyStockReadPort legacyStockReadPort,
        IStockConsequenceUnitOfWork consequenceUnitOfWork,
        IUnitOfWork unitOfWork,
        IStockLedgerScopeStateRepo scopeStateRepo,
        IStockSourceIdempotencyRepo idempotencyRepo)
    {
        _claimService = claimService;
        _legacyStockReadPort = legacyStockReadPort;
        _consequenceUnitOfWork = consequenceUnitOfWork;
        _unitOfWork = unitOfWork;
        _scopeStateRepo = scopeStateRepo;
        _idempotencyRepo = idempotencyRepo;
    }

    public Task<ReconstructStockLedgerBaselineResult> Handle(
        ReconstructStockLedgerBaselineCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));
        Guard.Against.NullOrWhiteSpace(request.ReceiptSourceId, nameof(request.ReceiptSourceId));

        var scopeKey = StockLedgerScopeKeyType.Create(request.BrgId, request.ReceiptSourceId);

        var terminal = TryTerminalShortCircuit(scopeKey);
        if (terminal is not null)
            return Task.FromResult(terminal);

        // Phase A — short claim TX (committed before any history read).
        var claim = _claimService.Claim(scopeKey);
        if (claim.ScopeState.ReconstructionStatus != ReconstructionStatusEnum.Reconstructing)
        {
            // Concurrent completion/inconsistency while claiming — re-evaluate terminal states.
            var afterClaim = TryTerminalShortCircuit(scopeKey);
            if (afterClaim is not null)
                return Task.FromResult(afterClaim);

            throw StockLedgerPersistenceException.Integrity(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) claim did not leave " +
                $"Reconstruction Status '{ReconstructionStatusEnum.Reconstructing}'.");
        }

        // Claimed or AlreadyClaimed+Reconstructing: respect claim and continue Phase B/C.
        for (var attempt = 1; attempt <= MaxBasisChangeRetries; attempt++)
        {
            var phaseB = ExecutePhaseB(scopeKey);
            var phaseC = ExecutePhaseC(scopeKey, phaseB);

            if (phaseC.Outcome == PhaseCOutcomeEnum.BasisChanged && attempt < MaxBasisChangeRetries)
                continue;

            return Task.FromResult(MapPhaseCResult(phaseC));
        }

        // Unreachable — loop always returns on last attempt.
        var stuck = LoadRequiredScope(scopeKey);
        return Task.FromResult(ReconstructStockLedgerBaselineResult.BasisChangedRetryRequired(stuck));
    }

    private ReconstructStockLedgerBaselineResult? TryTerminalShortCircuit(IStockLedgerScopeKey scopeKey)
    {
        var loaded = _scopeStateRepo.LoadEntity(scopeKey);
        if (!loaded.HasValue)
            return null;

        var scope = loaded.Value;
        return scope.ReconstructionStatus switch
        {
            ReconstructionStatusEnum.Reconstructed
                => ReconstructStockLedgerBaselineResult.AlreadyReconstructed(scope),
            ReconstructionStatusEnum.Inconsistent
                => ReconstructStockLedgerBaselineResult.AlreadyInconsistent(scope),
            _ => null
        };
    }

    /// <summary>
    /// Phase B — read legacy snapshot, capture basis fingerprint, calculate baseline.
    /// Must not open a write transaction.
    /// </summary>
    private PhaseBSnapshot ExecutePhaseB(StockLedgerScopeKeyType scopeKey)
    {
        var balances = _legacyStockReadPort.ListCurrentBalances(scopeKey);
        var journals = _legacyStockReadPort.ListJournalEntries(scopeKey);
        var basis = LegacyReconstructionBasisCalculator.Compute(balances, journals);
        var calculation = LegacyReconstructionBaselineCalculator.Calculate(scopeKey, balances, journals);
        return new PhaseBSnapshot(basis, calculation);
    }

    private PhaseCResult ExecutePhaseC(StockLedgerScopeKeyType scopeKey, PhaseBSnapshot phaseB)
    {
        // Revalidate legacy basis outside the write TX first (fresh read), then persist in short TX.
        var reBalances = _legacyStockReadPort.ListCurrentBalances(scopeKey);
        var reJournals = _legacyStockReadPort.ListJournalEntries(scopeKey);
        var reBasis = LegacyReconstructionBasisCalculator.Compute(reBalances, reJournals);

        if (!phaseB.Basis.Equals(reBasis))
        {
            var scope = LoadRequiredScope(scopeKey);
            return PhaseCResult.BasisChanged(scope);
        }

        // Claim must still be Reconstructing before we persist.
        var current = LoadRequiredScope(scopeKey);
        if (current.ReconstructionStatus != ReconstructionStatusEnum.Reconstructing)
        {
            var terminal = TryTerminalShortCircuit(scopeKey);
            if (terminal is not null)
                return PhaseCResult.FromTerminal(terminal);

            throw StockLedgerPersistenceException.Integrity(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) left Reconstructing " +
                $"before Phase C (status '{current.ReconstructionStatus}').");
        }

        if (phaseB.Calculation.IsInconsistent)
            return PersistInconsistent(scopeKey, phaseB.Calculation.InconsistencyReason!);

        return PersistBalanced(scopeKey, phaseB);
    }

    private PhaseCResult PersistInconsistent(StockLedgerScopeKeyType scopeKey, string reason)
    {
        using var tx = _unitOfWork.Begin();

        var current = LoadRequiredScope(scopeKey);
        if (current.ReconstructionStatus != ReconstructionStatusEnum.Reconstructing)
        {
            var terminal = TryTerminalShortCircuit(scopeKey);
            if (terminal is not null)
                return PhaseCResult.FromTerminal(terminal);

            throw StockLedgerPersistenceException.Integrity(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) cannot mark Inconsistent " +
                $"from status '{current.ReconstructionStatus}'.");
        }

        var next = current.MarkReconstructionInconsistent(reason);
        if (!_scopeStateRepo.TryUpdateWhenReconstructionStatus(
                next,
                ReconstructionStatusEnum.Reconstructing))
        {
            var afterRace = LoadRequiredScope(scopeKey);
            var terminal = TryTerminalShortCircuit(scopeKey);
            if (terminal is not null)
                return PhaseCResult.FromTerminal(terminal);

            throw StockLedgerPersistenceException.Concurrency(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) inconsistent mark lost the " +
                $"Reconstructing claim (status '{afterRace.ReconstructionStatus}').");
        }

        tx.Complete();
        return PhaseCResult.Inconsistent(next);
    }

    private PhaseCResult PersistBalanced(StockLedgerScopeKeyType scopeKey, PhaseBSnapshot phaseB)
    {
        var calculation = phaseB.Calculation;
        var completedScope = LoadRequiredScope(scopeKey)
            .CompleteReconstruction(
                phaseB.Basis,
                LegacyReconstructionBasisCalculator.AlgorithmVersion);

        var processedAt = DateTime.Now;
        var idempotencyKey = BuildReconstructionIdempotencyKey(scopeKey);

        // Empty balanced baseline: Scope only (no Movement/Layers).
        if (calculation.EstablishingMovement is null)
            return PersistEmptyBalanced(scopeKey, completedScope, idempotencyKey, processedAt);

        try
        {
            var draft = new StockConsequenceDraft(
                IdempotencyKey: idempotencyKey,
                ProcessedAt: processedAt,
                Movement: calculation.EstablishingMovement,
                Positions: calculation.ProposedPositions,
                ScopeState: completedScope,
                LegacyWrite: null,
                IdempotencyKind: StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                ExpectedPriorReconstructionStatus: ReconstructionStatusEnum.Reconstructing,
                LayerLegacyBindings: calculation.ProposedBindings.Count == 0
                    ? null
                    : calculation.ProposedBindings);

            var commit = _consequenceUnitOfWork.Commit(draft);
            if (commit.Outcome == StockConsequenceCommitOutcomeEnum.AlreadyCommitted)
            {
                var existing = LoadRequiredScope(scopeKey);
                if (existing.ReconstructionStatus == ReconstructionStatusEnum.Reconstructed)
                    return PhaseCResult.AlreadyReconstructed(existing);

                // Idempotency row exists but Scope not yet Reconstructed — unusual partial.
                // Fail closed without inventing a second baseline.
                throw StockLedgerPersistenceException.Integrity(
                    $"Reconstruction idempotency key '{idempotencyKey}' already exists but Scope " +
                    $"({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) is '{existing.ReconstructionStatus}'.");
            }

            return PhaseCResult.Reconstructed(completedScope);
        }
        catch (StockLedgerPersistenceException ex) when (ex.Code == "CONCURRENCY_CONFLICT")
        {
            var afterRace = LoadRequiredScope(scopeKey);
            var terminal = TryTerminalShortCircuit(scopeKey);
            if (terminal is not null)
                return PhaseCResult.FromTerminal(terminal);

            throw;
        }
    }

    private PhaseCResult PersistEmptyBalanced(
        StockLedgerScopeKeyType scopeKey,
        StockLedgerScopeStateModel completedScope,
        string idempotencyKey,
        DateTime processedAt)
    {
        using var tx = _unitOfWork.Begin();

        var idempotency = StockSourceIdempotencyModel.Create(
            StockSourceIdempotencyKindEnum.ReconstructionBaseline,
            idempotencyKey,
            processedAt,
            sourceTransactionId: idempotencyKey,
            stockMovementId: string.Empty,
            brgId: scopeKey.BrgId,
            receiptSourceId: scopeKey.ReceiptSourceId);

        var insert = _idempotencyRepo.InsertOrGetExisting(idempotency);
        if (!insert.WasInserted)
        {
            // Do not Complete — leave no new rows from this attempt.
            var existing = LoadRequiredScope(scopeKey);
            if (existing.ReconstructionStatus == ReconstructionStatusEnum.Reconstructed)
                return PhaseCResult.AlreadyReconstructed(existing);

            throw StockLedgerPersistenceException.Integrity(
                $"Reconstruction idempotency key '{idempotencyKey}' already exists but Scope " +
                $"({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) is '{existing.ReconstructionStatus}'.");
        }

        if (!_scopeStateRepo.TryUpdateWhenReconstructionStatus(
                completedScope,
                ReconstructionStatusEnum.Reconstructing))
        {
            throw StockLedgerPersistenceException.Concurrency(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) empty-baseline persist lost the " +
                "Reconstructing claim.");
        }

        tx.Complete();
        return PhaseCResult.Reconstructed(completedScope);
    }

    private StockLedgerScopeStateModel LoadRequiredScope(IStockLedgerScopeKey scopeKey)
    {
        var loaded = _scopeStateRepo.LoadEntity(scopeKey);
        if (!loaded.HasValue)
        {
            throw StockLedgerPersistenceException.Integrity(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) is missing after Phase A claim.");
        }

        return loaded.Value;
    }

    private static string BuildReconstructionIdempotencyKey(IStockLedgerScopeKey scopeKey)
        => $"RECON|{scopeKey.BrgId}|{scopeKey.ReceiptSourceId}|baseline";

    private static ReconstructStockLedgerBaselineResult MapPhaseCResult(PhaseCResult phaseC)
        => phaseC.Outcome switch
        {
            PhaseCOutcomeEnum.Reconstructed
                => ReconstructStockLedgerBaselineResult.Reconstructed(phaseC.ScopeState),
            PhaseCOutcomeEnum.Inconsistent
                => ReconstructStockLedgerBaselineResult.Inconsistent(phaseC.ScopeState),
            PhaseCOutcomeEnum.AlreadyReconstructed
                => ReconstructStockLedgerBaselineResult.AlreadyReconstructed(phaseC.ScopeState),
            PhaseCOutcomeEnum.AlreadyInconsistent
                => ReconstructStockLedgerBaselineResult.AlreadyInconsistent(phaseC.ScopeState),
            PhaseCOutcomeEnum.BasisChanged
                => ReconstructStockLedgerBaselineResult.BasisChangedRetryRequired(phaseC.ScopeState),
            _
                => throw new InvalidOperationException($"Unexpected Phase C outcome '{phaseC.Outcome}'.")
        };

    private sealed record PhaseBSnapshot(
        SynchronizationPositionType Basis,
        ReconstructionBaselineCalculationResult Calculation);

    private enum PhaseCOutcomeEnum
    {
        Reconstructed = 1,
        Inconsistent = 2,
        AlreadyReconstructed = 3,
        AlreadyInconsistent = 4,
        BasisChanged = 5
    }

    private sealed record PhaseCResult(
        PhaseCOutcomeEnum Outcome,
        StockLedgerScopeStateModel ScopeState)
    {
        public static PhaseCResult Reconstructed(StockLedgerScopeStateModel scope)
            => new(PhaseCOutcomeEnum.Reconstructed, scope);

        public static PhaseCResult Inconsistent(StockLedgerScopeStateModel scope)
            => new(PhaseCOutcomeEnum.Inconsistent, scope);

        public static PhaseCResult AlreadyReconstructed(StockLedgerScopeStateModel scope)
            => new(PhaseCOutcomeEnum.AlreadyReconstructed, scope);

        public static PhaseCResult AlreadyInconsistent(StockLedgerScopeStateModel scope)
            => new(PhaseCOutcomeEnum.AlreadyInconsistent, scope);

        public static PhaseCResult BasisChanged(StockLedgerScopeStateModel scope)
            => new(PhaseCOutcomeEnum.BasisChanged, scope);

        public static PhaseCResult FromTerminal(ReconstructStockLedgerBaselineResult terminal)
            => terminal.Outcome switch
            {
                ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed
                    => AlreadyReconstructed(terminal.ScopeState),
                ReconstructStockLedgerBaselineOutcomeEnum.AlreadyInconsistent
                    => AlreadyInconsistent(terminal.ScopeState),
                ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed
                    => Reconstructed(terminal.ScopeState),
                ReconstructStockLedgerBaselineOutcomeEnum.Inconsistent
                    => Inconsistent(terminal.ScopeState),
                _
                    => throw new InvalidOperationException(
                        $"Unexpected terminal outcome '{terminal.Outcome}' during Phase C.")
            };
    }
}
