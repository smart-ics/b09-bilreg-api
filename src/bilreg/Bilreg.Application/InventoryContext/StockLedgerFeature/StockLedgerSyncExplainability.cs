using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S8 / initial G-24 — Structured explainability for a sync or Freshness Gate execution.
/// Assembled at the call boundary from persisted Scope state plus the discovery (and optional
/// reconcile) outcomes used in that execution. Does not persist last-outcome columns;
/// post-hoc diagnostics re-invoke read-only ports via <see cref="FromPersistedScopeEvaluation"/>.
/// </summary>
public sealed record StockLedgerSyncExplainability(
    string AlgorithmVersion,
    SynchronizationStateEnum SynchronizationState,
    string? InconsistencyReason,
    LegacyChangeDiscoveryOutcomeEnum? DiscoveryOutcome,
    string? DiscoveryExplanation,
    StockReconciliationOutcomeEnum? ReconcileOutcome,
    string? ReconcileExplanation)
{
    /// <summary>
    /// Assembles explainability from Scope after an execution attempt plus the discovery
    /// (and optional reconcile) results used in that attempt. <paramref name="discovery"/>
    /// may be null when discovery was not evaluated (precondition failure).
    /// </summary>
    public static StockLedgerSyncExplainability FromExecution(
        StockLedgerScopeStateModel scope,
        LegacyChangeDiscoveryResult? discovery,
        StockReconciliationResult? reconcile)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return new StockLedgerSyncExplainability(
            AlgorithmVersion: ResolveAlgorithmVersion(scope),
            SynchronizationState: scope.SynchronizationState,
            InconsistencyReason: scope.InconsistencyReason,
            DiscoveryOutcome: discovery?.Outcome,
            DiscoveryExplanation: discovery?.Explanation,
            ReconcileOutcome: reconcile?.Outcome,
            ReconcileExplanation: reconcile?.Explanation);
    }

    /// <summary>
    /// Post-hoc diagnostics: combine persisted Scope with freshly evaluated discovery and
    /// reconcile results (read-only ports; no sync). Prefer this over inventing durable
    /// last-outcome columns.
    /// </summary>
    public static StockLedgerSyncExplainability FromPersistedScopeEvaluation(
        StockLedgerScopeStateModel scope,
        LegacyChangeDiscoveryResult discovery,
        StockReconciliationResult reconcile)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(discovery);
        ArgumentNullException.ThrowIfNull(reconcile);

        return FromExecution(scope, discovery, reconcile);
    }

    private static string ResolveAlgorithmVersion(StockLedgerScopeStateModel scope)
    {
        var stored = scope.SynchronizationPosition?.AlgorithmVersion;
        if (!string.IsNullOrWhiteSpace(stored))
            return stored;

        return LegacyReconstructionBasisCalculator.AlgorithmVersion;
    }
}
