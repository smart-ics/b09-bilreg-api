using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.Shared;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;

/// <summary>
/// P3-S4 / P3-S6 — Incremental Legacy catch-up for one reconstructed Item + Receipt Source scope.
/// Thin orchestration: discover → interpret → short TX apply → reconcile → advance position
/// only when material reconciliation permits. P3-S6 adds sync-specific bounded retry for
/// claim/OCC conflicts. Does not rewrite <c>tb_stok</c> / <c>tb_buku</c>.
/// </summary>
public sealed record SynchronizeStockLedgerScopeCommand(
    string BrgId,
    string ReceiptSourceId) : IRequest<SynchronizeStockLedgerScopeResult>, IStockLedgerScopeKey;

public enum SynchronizeStockLedgerScopeOutcomeEnum
{
    /// <summary>Fingerprint unchanged and discovery identity coverage already complete.</summary>
    AlreadyCurrent = 1,

    /// <summary>Catch-up committed; Synchronization Position advanced; Scope Current.</summary>
    Synchronized = 2,

    /// <summary>Material mismatch / ambiguity / undeterminable — Scope Inconsistent; prior position retained.</summary>
    Inconsistent = 3,

    /// <summary>Bounded re-derive cannot run safely — fail closed without erasing history.</summary>
    RequiresScopedReDerive = 4,

    /// <summary>
    /// Concurrent sync claim lost or OCC conflict after bounded sync-specific retries exhausted.
    /// </summary>
    ClaimConflict = 5
}

public sealed record SynchronizeStockLedgerScopeResult(
    SynchronizeStockLedgerScopeOutcomeEnum Outcome,
    StockLedgerScopeStateModel ScopeState,
    string? Explanation,
    SynchronizationPositionType? SynchronizationPosition)
{
    public static SynchronizeStockLedgerScopeResult AlreadyCurrent(StockLedgerScopeStateModel scope)
        => new(
            SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent,
            scope,
            Explanation: null,
            SynchronizationPosition: scope.SynchronizationPosition);

    public static SynchronizeStockLedgerScopeResult Synchronized(StockLedgerScopeStateModel scope)
        => new(
            SynchronizeStockLedgerScopeOutcomeEnum.Synchronized,
            scope,
            Explanation: null,
            SynchronizationPosition: scope.SynchronizationPosition);

    public static SynchronizeStockLedgerScopeResult Inconsistent(
        StockLedgerScopeStateModel scope,
        string explanation)
        => new(
            SynchronizeStockLedgerScopeOutcomeEnum.Inconsistent,
            scope,
            explanation,
            SynchronizationPosition: scope.SynchronizationPosition);

    public static SynchronizeStockLedgerScopeResult RequiresScopedReDerive(
        StockLedgerScopeStateModel scope,
        string explanation)
        => new(
            SynchronizeStockLedgerScopeOutcomeEnum.RequiresScopedReDerive,
            scope,
            explanation,
            SynchronizationPosition: scope.SynchronizationPosition);

    public static SynchronizeStockLedgerScopeResult ClaimConflict(StockLedgerScopeStateModel scope)
        => new(
            SynchronizeStockLedgerScopeOutcomeEnum.ClaimConflict,
            scope,
            "Another process holds SynchronizationRequired or Scope sync state changed concurrently "
            + $"(exhausted {SynchronizeStockLedgerScopeHandler.MaxSyncConflictRetries} sync-specific retries).",
            SynchronizationPosition: scope.SynchronizationPosition);
}

public sealed class SynchronizeStockLedgerScopeHandler
    : IRequestHandler<SynchronizeStockLedgerScopeCommand, SynchronizeStockLedgerScopeResult>
{
    /// <summary>
    /// P3-S6 — Bounded sync-specific retries for claim/OCC conflicts.
    /// Mirrors reconstruction <c>MaxBasisChangeRetries</c>; not a generic retry framework.
    /// </summary>
    public const int MaxSyncConflictRetries = 3;

    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;
    private readonly ILegacyChangeDiscoveryPort _discoveryPort;
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly IStockReconciliationPort _reconciliationPort;
    private readonly IStockConsequenceUnitOfWork _consequenceUnitOfWork;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SynchronizationClaimService _claimService;
    private readonly LegacySyncLedgerSnapshotLoader _snapshotLoader;
    private readonly LegacySyncIdentityBootstrapper _identityBootstrapper;
    private readonly IStockPositionRepo _positionRepo;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;

    public SynchronizeStockLedgerScopeHandler(
        IStockLedgerScopeStateRepo scopeStateRepo,
        ILegacyChangeDiscoveryPort discoveryPort,
        ILegacyStockReadPort legacyStockReadPort,
        IStockReconciliationPort reconciliationPort,
        IStockConsequenceUnitOfWork consequenceUnitOfWork,
        IUnitOfWork unitOfWork,
        SynchronizationClaimService claimService,
        LegacySyncLedgerSnapshotLoader snapshotLoader,
        LegacySyncIdentityBootstrapper identityBootstrapper,
        IStockPositionRepo positionRepo,
        IStockSourceIdempotencyRepo idempotencyRepo)
    {
        _scopeStateRepo = scopeStateRepo;
        _discoveryPort = discoveryPort;
        _legacyStockReadPort = legacyStockReadPort;
        _reconciliationPort = reconciliationPort;
        _consequenceUnitOfWork = consequenceUnitOfWork;
        _unitOfWork = unitOfWork;
        _claimService = claimService;
        _snapshotLoader = snapshotLoader;
        _identityBootstrapper = identityBootstrapper;
        _positionRepo = positionRepo;
        _idempotencyRepo = idempotencyRepo;
    }

    public Task<SynchronizeStockLedgerScopeResult> Handle(
        SynchronizeStockLedgerScopeCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));
        Guard.Against.NullOrWhiteSpace(request.ReceiptSourceId, nameof(request.ReceiptSourceId));

        var scopeKey = StockLedgerScopeKeyType.Create(request.BrgId, request.ReceiptSourceId);

        SynchronizeStockLedgerScopeResult? lastConflict = null;
        for (var attempt = 1; attempt <= MaxSyncConflictRetries; attempt++)
        {
            // Attempt 1: strict claim (no resume). Attempt 2+: allow crash-resume of
            // SynchronizationRequired left by a prior failed TX within this request boundary.
            var allowResume = attempt > 1;
            var result = ExecuteOnce(scopeKey, allowResume);

            if (result.Outcome != SynchronizeStockLedgerScopeOutcomeEnum.ClaimConflict)
                return Task.FromResult(result);

            lastConflict = result;
        }

        return Task.FromResult(lastConflict!);
    }

    /// <summary>
    /// One synchronization boundary attempt: reload persisted Scope + rediscover via fingerprint-v1.
    /// Does not recurse into a second catch-up cycle.
    /// </summary>
    private SynchronizeStockLedgerScopeResult ExecuteOnce(
        StockLedgerScopeKeyType scopeKey,
        bool allowResume)
    {
        var scope = LoadRequiredScope(scopeKey);
        var precondition = ValidatePreconditions(scope);
        if (precondition is not null)
            return precondition;

        // Outside TX — discovery + legacy snapshot (re-read every attempt; no in-memory sync cache).
        var discovery = _discoveryPort.DiscoverChanges(scopeKey, scope.SynchronizationPosition);
        var balances = _legacyStockReadPort.ListCurrentBalances(scopeKey);
        var journals = _legacyStockReadPort.ListJournalEntries(scopeKey);
        var existingKeys = _idempotencyRepo.ListSyncIdentityRecordsForScope(scopeKey)
            .Select(r => r.IdempotencyKey)
            .ToList();
        var coverageComplete = LegacySyncIdentityBootstrapper.HasCompleteCoverage(
            scopeKey,
            journals,
            balances,
            existingKeys);

        if (discovery.Outcome == LegacyChangeDiscoveryOutcomeEnum.Undeterminable)
        {
            return FailClosedInconsistent(
                scope,
                discovery.Explanation
                ?? "Discovery outcome is Undeterminable; Synchronization Position not advanced.");
        }

        // Fast path: unchanged + keys complete — no claim needed.
        if (discovery.Outcome == LegacyChangeDiscoveryOutcomeEnum.Unchanged && coverageComplete)
            return SynchronizeStockLedgerScopeResult.AlreadyCurrent(scope);

        // Short TX — claim SynchronizationRequired when work is needed.
        var claim = _claimService.Claim(scopeKey, allowResume);
        if (claim.Outcome != SynchronizationClaimOutcomeEnum.Claimed)
            return SynchronizeStockLedgerScopeResult.ClaimConflict(claim.ScopeState);

        scope = claim.ScopeState;
        var processedAt = DateTime.Now;

        try
        {
            var applyOutcome = ApplyCatchUp(
                scopeKey,
                scope,
                discovery,
                journals,
                balances,
                coverageComplete,
                processedAt);

            if (applyOutcome.Terminal is not null)
                return applyOutcome.Terminal;

            // Outside TX — material reconcile (may overlay PendingSynchronization).
            var reconcile = _reconciliationPort.Reconcile(scopeKey);
            if (!StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(reconcile))
            {
                return FailClosedInconsistent(
                    LoadRequiredScope(scopeKey),
                    reconcile.Explanation
                    ?? $"Material reconciliation outcome '{reconcile.Outcome}' does not permit Synchronization Position advance.");
            }

            // Fresh authority snapshot for fingerprint-v1 position advancement.
            var postBalances = _legacyStockReadPort.ListCurrentBalances(scopeKey);
            var postJournals = _legacyStockReadPort.ListJournalEntries(scopeKey);
            var newPosition = LegacyReconstructionBasisCalculator.Compute(postBalances, postJournals);

            return CompleteSynchronization(scopeKey, newPosition);
        }
        catch (StockLedgerPersistenceException ex) when (ex.Code == "CONCURRENCY_CONFLICT")
        {
            return SynchronizeStockLedgerScopeResult.ClaimConflict(LoadRequiredScope(scopeKey));
        }
    }

    private ApplyCatchUpResult ApplyCatchUp(
        StockLedgerScopeKeyType scopeKey,
        StockLedgerScopeStateModel scope,
        LegacyChangeDiscoveryResult discovery,
        IReadOnlyList<LegacyStockJournalEntryType> journals,
        IReadOnlyList<LegacyStockBalanceType> balances,
        bool coverageComplete,
        DateTime processedAt)
    {
        var onlyRequiresReDerive = discovery.Deltas.Count > 0
            && discovery.Deltas.All(d => d.Kind == LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive);

        // R-001 — bootstrap when coverage incomplete (Unchanged or sole RequiresScopedReDerive / no keys).
        if (!coverageComplete
            && (discovery.Outcome == LegacyChangeDiscoveryOutcomeEnum.Unchanged
                || onlyRequiresReDerive))
        {
            _identityBootstrapper.Bootstrap(scopeKey, journals, balances, processedAt);
            return ApplyCatchUpResult.Continue();
        }

        if (onlyRequiresReDerive)
        {
            return ApplyCatchUpResult.Done(SynchronizeStockLedgerScopeResult.RequiresScopedReDerive(
                scope,
                discovery.Deltas[0].Explanation
                ?? discovery.Explanation
                ?? "Discovery requires scoped re-derive; bounded bootstrap path is not available."));
        }

        if (discovery.Outcome == LegacyChangeDiscoveryOutcomeEnum.Unchanged)
            return ApplyCatchUpResult.Continue();

        // ChangesDetected with classifiable deltas → interpret + apply.
        var snapshot = _snapshotLoader.Load(scopeKey);
        var interpretation = LegacySyncDeltaInterpreter.Interpret(
            scopeKey,
            discovery,
            snapshot,
            journals,
            balances);

        if (interpretation.Outcome == LegacySyncInterpretationOutcomeEnum.RequiresScopedReDerive)
        {
            return ApplyCatchUpResult.Done(SynchronizeStockLedgerScopeResult.RequiresScopedReDerive(
                scope,
                interpretation.Explanation
                ?? "Interpreter requires scoped re-derive; Synchronization Position not advanced."));
        }

        if (interpretation.Outcome == LegacySyncInterpretationOutcomeEnum.Ambiguous)
        {
            return ApplyCatchUpResult.Done(FailClosedInconsistent(
                scope,
                interpretation.Explanation
                ?? "Interpreter returned Ambiguous; Synchronization Position not advanced."));
        }

        PersistIntents(scopeKey, interpretation.Intents, processedAt);
        return ApplyCatchUpResult.Continue();
    }

    private void PersistIntents(
        IStockLedgerScopeKey scopeKey,
        IReadOnlyList<LegacySyncIntentType> intents,
        DateTime processedAt)
    {
        var positionsByLocation = _positionRepo.ListByLedgerScope(scopeKey)
            .ToDictionary(p => p.LayananId, StringComparer.Ordinal);

        // Composition hygiene: when set-diff emits both JournalInsert (new layer) and
        // BalanceUpdate for the same location, applying both would double-count quantity.
        // Prefer the establishing receipt/outbound; skip redundant layer adjustments.
        var locationsWithEstablishment = intents
            .Where(i => i.Kind is LegacySyncIntentKindEnum.ApplyLegacySynchronizedReceipt
                or LegacySyncIntentKindEnum.ApplyLegacySynchronizedOutbound)
            .Select(i => i.LayananId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        // After ReversePriorMovement depletes layers, a BalanceDelete Adjust for the same
        // location would try to deplete again from the pre-apply snapshot qty.
        var locationsWithReversal = intents
            .Where(i => i.Kind == LegacySyncIntentKindEnum.ReversePriorMovement)
            .Select(i => i.LayananId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var intent in intents)
        {
            if (intent.Kind == LegacySyncIntentKindEnum.AdjustLayerRemainingQuantity
                && !string.IsNullOrWhiteSpace(intent.LayananId)
                && (locationsWithEstablishment.Contains(intent.LayananId)
                    || locationsWithReversal.Contains(intent.LayananId)))
            {
                // Still persist SyncBatch identity for the balance key (coverage), quantity-neutral.
                _consequenceUnitOfWork.CommitSyncEvidence(new StockSyncEvidenceDraft(
                    IdempotencyKey: intent.SyncIdempotencyKey,
                    ProcessedAt: processedAt,
                    BrgId: scopeKey.BrgId,
                    ReceiptSourceId: scopeKey.ReceiptSourceId,
                    StockMovementId: intent.TargetMovementId,
                    SourceTransactionId: intent.LegacyRowId));
                continue;
            }

            var applied = LegacySyncIntentApplicator.Apply(intent, scopeKey, positionsByLocation);

            if (intent.Kind == LegacySyncIntentKindEnum.RepresentationalBalanceOmission
                || applied.Movement is null)
            {
                _consequenceUnitOfWork.CommitSyncEvidence(new StockSyncEvidenceDraft(
                    IdempotencyKey: intent.SyncIdempotencyKey,
                    ProcessedAt: processedAt,
                    BrgId: scopeKey.BrgId,
                    ReceiptSourceId: scopeKey.ReceiptSourceId,
                    StockMovementId: intent.TargetMovementId,
                    SourceTransactionId: intent.LegacyJournalId ?? intent.LegacyRowId));
                continue;
            }

            _consequenceUnitOfWork.Commit(new StockConsequenceDraft(
                IdempotencyKey: intent.SyncIdempotencyKey,
                ProcessedAt: processedAt,
                Movement: applied.Movement,
                Positions: applied.Positions,
                ScopeState: null,
                LegacyWrite: null,
                IdempotencyKind: StockSourceIdempotencyKindEnum.SyncBatch));
        }
    }

    private SynchronizeStockLedgerScopeResult CompleteSynchronization(
        IStockLedgerScopeKey scopeKey,
        SynchronizationPositionType newPosition)
    {
        using var tx = _unitOfWork.Begin();

        var current = LoadRequiredScope(scopeKey);
        if (current.SynchronizationState != SynchronizationStateEnum.SynchronizationRequired)
        {
            throw StockLedgerPersistenceException.Concurrency(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) is '{current.SynchronizationState}' " +
                "at CompleteSynchronization; expected SynchronizationRequired.");
        }

        var completed = current.CompleteSynchronization(newPosition);
        if (!_scopeStateRepo.TryUpdateWhenSynchronizationState(
                completed,
                SynchronizationStateEnum.SynchronizationRequired))
        {
            throw StockLedgerPersistenceException.Concurrency(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) lost SynchronizationRequired claim " +
                "at CompleteSynchronization.");
        }

        tx.Complete();
        return SynchronizeStockLedgerScopeResult.Synchronized(completed);
    }

    private SynchronizeStockLedgerScopeResult FailClosedInconsistent(
        StockLedgerScopeStateModel scope,
        string reason)
    {
        if (scope.SynchronizationState is not (
                SynchronizationStateEnum.LegacyChangePending
                or SynchronizationStateEnum.SynchronizationRequired))
        {
            return SynchronizeStockLedgerScopeResult.Inconsistent(scope, reason);
        }

        var priorPosition = scope.SynchronizationPosition;
        var inconsistent = scope.MarkSynchronizationInconsistent(reason);

        using var tx = _unitOfWork.Begin();
        if (!_scopeStateRepo.TryUpdateWhenSynchronizationState(
                inconsistent,
                scope.SynchronizationState))
        {
            var raced = LoadRequiredScope(scope);
            return SynchronizeStockLedgerScopeResult.ClaimConflict(raced);
        }

        tx.Complete();

        // Domain MarkSynchronizationInconsistent retains position; assert invariant for callers.
        if (!Equals(inconsistent.SynchronizationPosition, priorPosition))
        {
            throw StockLedgerPersistenceException.Integrity(
                "MarkSynchronizationInconsistent must not alter Synchronization Position.");
        }

        return SynchronizeStockLedgerScopeResult.Inconsistent(inconsistent, reason);
    }

    private static SynchronizeStockLedgerScopeResult? ValidatePreconditions(
        StockLedgerScopeStateModel scope)
    {
        if (scope.ReconstructionStatus != ReconstructionStatusEnum.Reconstructed)
        {
            return SynchronizeStockLedgerScopeResult.Inconsistent(
                scope,
                $"Scope must be Reconstructed before synchronization (status '{scope.ReconstructionStatus}').");
        }

        if (scope.SynchronizationPosition is null)
        {
            return SynchronizeStockLedgerScopeResult.Inconsistent(
                scope,
                "Scope has no Synchronization Position; reconstruct baseline before catch-up.");
        }

        if (!string.Equals(
                scope.SynchronizationPosition.AlgorithmVersion,
                LegacyReconstructionBasisCalculator.AlgorithmVersion,
                StringComparison.Ordinal))
        {
            return SynchronizeStockLedgerScopeResult.Inconsistent(
                scope,
                $"Stored algorithm version '{scope.SynchronizationPosition.AlgorithmVersion}' does not match "
                + $"'{LegacyReconstructionBasisCalculator.AlgorithmVersion}'.");
        }

        if (scope.SynchronizationState == SynchronizationStateEnum.Inconsistent)
        {
            return SynchronizeStockLedgerScopeResult.Inconsistent(
                scope,
                scope.InconsistencyReason ?? "Scope Synchronization State is already Inconsistent.");
        }

        return null;
    }

    private StockLedgerScopeStateModel LoadRequiredScope(IStockLedgerScopeKey scopeKey)
    {
        var loaded = _scopeStateRepo.LoadEntity(scopeKey);
        if (!loaded.HasValue)
        {
            throw StockLedgerPersistenceException.Integrity(
                $"Scope ({scopeKey.BrgId}/{scopeKey.ReceiptSourceId}) was not found.");
        }

        return loaded.Value;
    }

    private readonly struct ApplyCatchUpResult
    {
        public SynchronizeStockLedgerScopeResult? Terminal { get; }

        private ApplyCatchUpResult(SynchronizeStockLedgerScopeResult? terminal)
            => Terminal = terminal;

        public static ApplyCatchUpResult Continue() => new(null);
        public static ApplyCatchUpResult Done(SynchronizeStockLedgerScopeResult result) => new(result);
    }
}
