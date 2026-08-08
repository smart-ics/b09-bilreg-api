using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S5 / G-12 — Legacy Freshness Gate.
/// Before a Ledger-dependent stock decision, proves the reconstructed scope is current
/// with Legacy Stock Authority (or fails closed / marks not current).
/// <para>
/// Replaces the rejected Authority Gate: never blocks VB6 and never rejects a legacy write
/// because the scope origin is Native/Reconstructed. Invokes catch-up
/// (<see cref="SynchronizeStockLedgerScopeHandler"/>) at most once per gate call.
/// </para>
/// Future Availability callers should honor <see cref="LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent"/>
/// (maps to <c>AvailabilityDiscoveryOutcomeEnum.StaleOrNotCurrent</c>) without treating provisional
/// G-08 discovery as Ledger authority.
/// </summary>
public sealed class LegacyStockFreshnessGate
{
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;
    private readonly ILegacyChangeDiscoveryPort _discoveryPort;
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;
    private readonly Func<SynchronizeStockLedgerScopeCommand, CancellationToken, Task<SynchronizeStockLedgerScopeResult>> _synchronize;

    public LegacyStockFreshnessGate(
        IStockLedgerScopeStateRepo scopeStateRepo,
        ILegacyChangeDiscoveryPort discoveryPort,
        ILegacyStockReadPort legacyStockReadPort,
        IStockSourceIdempotencyRepo idempotencyRepo,
        SynchronizeStockLedgerScopeHandler syncHandler)
        : this(
            scopeStateRepo,
            discoveryPort,
            legacyStockReadPort,
            idempotencyRepo,
            syncHandler is null
                ? throw new ArgumentNullException(nameof(syncHandler))
                : syncHandler.Handle)
    {
    }

    /// <summary>
    /// Test-friendly constructor: inject a sync delegate to assert at-most-once invocation
    /// without introducing a production interface.
    /// </summary>
    public LegacyStockFreshnessGate(
        IStockLedgerScopeStateRepo scopeStateRepo,
        ILegacyChangeDiscoveryPort discoveryPort,
        ILegacyStockReadPort legacyStockReadPort,
        IStockSourceIdempotencyRepo idempotencyRepo,
        Func<SynchronizeStockLedgerScopeCommand, CancellationToken, Task<SynchronizeStockLedgerScopeResult>> synchronize)
    {
        _scopeStateRepo = scopeStateRepo ?? throw new ArgumentNullException(nameof(scopeStateRepo));
        _discoveryPort = discoveryPort ?? throw new ArgumentNullException(nameof(discoveryPort));
        _legacyStockReadPort = legacyStockReadPort ?? throw new ArgumentNullException(nameof(legacyStockReadPort));
        _idempotencyRepo = idempotencyRepo ?? throw new ArgumentNullException(nameof(idempotencyRepo));
        _synchronize = synchronize ?? throw new ArgumentNullException(nameof(synchronize));
    }

    /// <summary>
    /// Ensures the scope is fresh enough to trust Stock Ledger layers for a subsequent decision.
    /// Optional <paramref name="decisionContext"/> is appended to explanations only — no branching.
    /// </summary>
    public async Task<LegacyStockFreshnessGateResult> EnsureFreshAsync(
        IStockLedgerScopeKey scopeKey,
        string? decisionContext = null,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(scopeKey, nameof(scopeKey));
        Guard.Against.NullOrWhiteSpace(scopeKey.BrgId, nameof(scopeKey.BrgId));
        Guard.Against.NullOrWhiteSpace(scopeKey.ReceiptSourceId, nameof(scopeKey.ReceiptSourceId));

        var key = StockLedgerScopeKeyType.Create(scopeKey.BrgId, scopeKey.ReceiptSourceId);
        var scope = LoadRequiredScope(key);

        var precondition = ValidatePreconditions(scope, decisionContext);
        if (precondition is not null)
            return precondition;

        // Outside TX — discovery for fast-path and explainability (same fingerprint-v1 path as catch-up).
        var discovery = _discoveryPort.DiscoverChanges(key, scope.SynchronizationPosition);

        if (discovery.Outcome == LegacyChangeDiscoveryOutcomeEnum.Undeterminable)
        {
            return FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent,
                scope,
                AppendContext(
                    discovery.Explanation
                    ?? "Discovery outcome is Undeterminable; freshness cannot be proven.",
                    decisionContext));
        }

        var balances = _legacyStockReadPort.ListCurrentBalances(key);
        var journals = _legacyStockReadPort.ListJournalEntries(key);
        var existingKeys = _idempotencyRepo.ListSyncIdentityRecordsForScope(key)
            .Select(r => r.IdempotencyKey)
            .ToList();
        var coverageComplete = LegacySyncIdentityBootstrapper.HasCompleteCoverage(
            key,
            journals,
            balances,
            existingKeys);

        // Fast path: unchanged + coverage complete — no catch-up this call.
        if (discovery.Outcome == LegacyChangeDiscoveryOutcomeEnum.Unchanged && coverageComplete)
        {
            return Pass(
                LegacyStockFreshnessGateOutcomeEnum.Current,
                scope,
                AppendContext(null, decisionContext));
        }

        // At most one catch-up boundary for this gate call (no nested rediscovery→sync loop).
        var syncResult = await _synchronize(
            new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId),
            cancellationToken);

        return MapSyncResult(syncResult, decisionContext);
    }

    private static LegacyStockFreshnessGateResult MapSyncResult(
        SynchronizeStockLedgerScopeResult syncResult,
        string? decisionContext)
    {
        return syncResult.Outcome switch
        {
            SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent => Pass(
                LegacyStockFreshnessGateOutcomeEnum.Current,
                syncResult.ScopeState,
                AppendContext(syncResult.Explanation, decisionContext)),

            SynchronizeStockLedgerScopeOutcomeEnum.Synchronized => Pass(
                LegacyStockFreshnessGateOutcomeEnum.SynchronizedNow,
                syncResult.ScopeState,
                AppendContext(syncResult.Explanation, decisionContext)),

            SynchronizeStockLedgerScopeOutcomeEnum.Inconsistent => FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.Inconsistent,
                syncResult.ScopeState,
                AppendContext(
                    syncResult.Explanation
                    ?? syncResult.ScopeState.InconsistencyReason
                    ?? "Synchronization marked scope Inconsistent.",
                    decisionContext)),

            SynchronizeStockLedgerScopeOutcomeEnum.RequiresScopedReDerive => FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent,
                syncResult.ScopeState,
                AppendContext(
                    syncResult.Explanation
                    ?? "Scoped re-derive required; Ledger layers are not safe to trust.",
                    decisionContext)),

            SynchronizeStockLedgerScopeOutcomeEnum.ClaimConflict => FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent,
                syncResult.ScopeState,
                AppendContext(
                    syncResult.Explanation
                    ?? "Concurrent synchronization claim conflict; retry on a later Freshness Gate call.",
                    decisionContext)),

            _ => FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent,
                syncResult.ScopeState,
                AppendContext(
                    $"Unexpected synchronization outcome '{syncResult.Outcome}'.",
                    decisionContext))
        };
    }

    private static LegacyStockFreshnessGateResult? ValidatePreconditions(
        StockLedgerScopeStateModel scope,
        string? decisionContext)
    {
        if (scope.ReconstructionStatus != ReconstructionStatusEnum.Reconstructed)
        {
            return FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.Inconsistent,
                scope,
                AppendContext(
                    $"Scope must be Reconstructed before Freshness Gate (status '{scope.ReconstructionStatus}').",
                    decisionContext));
        }

        if (scope.SynchronizationPosition is null)
        {
            return FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.Inconsistent,
                scope,
                AppendContext(
                    "Scope has no Synchronization Position; reconstruct baseline before Freshness Gate.",
                    decisionContext));
        }

        if (!string.Equals(
                scope.SynchronizationPosition.AlgorithmVersion,
                LegacyReconstructionBasisCalculator.AlgorithmVersion,
                StringComparison.Ordinal))
        {
            return FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.Inconsistent,
                scope,
                AppendContext(
                    $"Stored algorithm version '{scope.SynchronizationPosition.AlgorithmVersion}' does not match "
                    + $"'{LegacyReconstructionBasisCalculator.AlgorithmVersion}'.",
                    decisionContext));
        }

        if (scope.SynchronizationState == SynchronizationStateEnum.Inconsistent)
        {
            return FailClosed(
                LegacyStockFreshnessGateOutcomeEnum.Inconsistent,
                scope,
                AppendContext(
                    scope.InconsistencyReason
                    ?? "Scope Synchronization State is already Inconsistent.",
                    decisionContext));
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

    private static LegacyStockFreshnessGateResult Pass(
        LegacyStockFreshnessGateOutcomeEnum outcome,
        StockLedgerScopeStateModel scope,
        string? explanation)
        => new(
            outcome,
            scope,
            explanation,
            scope.SynchronizationPosition,
            IsSafeToTrustLedgerLayers: true);

    private static LegacyStockFreshnessGateResult FailClosed(
        LegacyStockFreshnessGateOutcomeEnum outcome,
        StockLedgerScopeStateModel scope,
        string? explanation)
        => new(
            outcome,
            scope,
            explanation,
            scope.SynchronizationPosition,
            IsSafeToTrustLedgerLayers: false);

    private static string? AppendContext(string? explanation, string? decisionContext)
    {
        if (string.IsNullOrWhiteSpace(decisionContext))
            return explanation;

        var context = $"DecisionContext: {decisionContext.Trim()}";
        return string.IsNullOrWhiteSpace(explanation)
            ? context
            : $"{explanation} ({context})";
    }
}

public enum LegacyStockFreshnessGateOutcomeEnum
{
    /// <summary>Fingerprint unchanged and coverage complete — no catch-up this call.</summary>
    Current = 1,

    /// <summary>Catch-up succeeded in this gate call; Synchronization Position advanced.</summary>
    SynchronizedNow = 2,

    /// <summary>
    /// Not safe to trust Ledger layers (undeterminable, claim conflict, scoped re-derive).
    /// Maps to <c>AvailabilityDiscoveryOutcomeEnum.StaleOrNotCurrent</c> for future callers.
    /// </summary>
    StaleOrNotCurrent = 3,

    /// <summary>Scope/material failure; prior Synchronization Position retained.</summary>
    Inconsistent = 4
}

public sealed record LegacyStockFreshnessGateResult(
    LegacyStockFreshnessGateOutcomeEnum Outcome,
    StockLedgerScopeStateModel ScopeState,
    string? Explanation,
    SynchronizationPositionType? SynchronizationPosition,
    bool IsSafeToTrustLedgerLayers);
