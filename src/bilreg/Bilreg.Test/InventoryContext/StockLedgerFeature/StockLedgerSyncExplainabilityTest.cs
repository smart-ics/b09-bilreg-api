using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S8 — Initial G-24 explainability factories (pure; no SQL).
/// Integration explainability assertions live on sync/gate happy-path and Inconsistent tests.
/// </summary>
public class StockLedgerSyncExplainabilityTest
{
    private static readonly BrgReff Item = new("BRG01", "Paracetamol");
    private static readonly ReceiptSourceType ReceiptSource = ReceiptSourceType.Create("DO001");

    [Fact]
    public void FromExecution_WithDiscoveryAndReconcile_MapsAllFields()
    {
        var scope = ReconstructedCurrentScope();
        var discovery = new LegacyChangeDiscoveryResult(
            LegacyChangeDiscoveryOutcomeEnum.ChangesDetected,
            scope.SynchronizationPosition,
            Array.Empty<LegacyDiscoveredDeltaType>(),
            "legacy journal insert detected");
        var reconcile = new StockReconciliationResult(
            StockReconciliationOutcomeEnum.PendingSynchronization,
            15m,
            15m,
            0m,
            Array.Empty<StockReconciliationDifferenceType>(),
            "material quantities match; sync pending overlay");

        var explain = StockLedgerSyncExplainability.FromExecution(scope, discovery, reconcile);

        explain.AlgorithmVersion.Should().Be(LegacyReconstructionBasisCalculator.AlgorithmVersion);
        explain.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        explain.InconsistencyReason.Should().BeNull();
        explain.DiscoveryOutcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.ChangesDetected);
        explain.DiscoveryExplanation.Should().Be("legacy journal insert detected");
        explain.ReconcileOutcome.Should().Be(StockReconciliationOutcomeEnum.PendingSynchronization);
        explain.ReconcileExplanation.Should().Be("material quantities match; sync pending overlay");
    }

    [Fact]
    public void FromExecution_WithoutReconcile_LeavesReconcileNull()
    {
        var scope = ReconstructedCurrentScope();
        var discovery = new LegacyChangeDiscoveryResult(
            LegacyChangeDiscoveryOutcomeEnum.Unchanged,
            scope.SynchronizationPosition,
            Array.Empty<LegacyDiscoveredDeltaType>(),
            Explanation: null);

        var explain = StockLedgerSyncExplainability.FromExecution(scope, discovery, reconcile: null);

        explain.DiscoveryOutcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.Unchanged);
        explain.ReconcileOutcome.Should().BeNull();
        explain.ReconcileExplanation.Should().BeNull();
    }

    [Fact]
    public void FromExecution_WithoutDiscovery_LeavesDiscoveryNull_UsesScopeFields()
    {
        var scope = ReconstructedCurrentScope()
            .RequireSynchronization()
            .MarkSynchronizationInconsistent("precondition already inconsistent");

        var explain = StockLedgerSyncExplainability.FromExecution(
            scope,
            discovery: null,
            reconcile: null);

        explain.AlgorithmVersion.Should().Be("fingerprint-v1");
        explain.SynchronizationState.Should().Be(SynchronizationStateEnum.Inconsistent);
        explain.InconsistencyReason.Should().Be("precondition already inconsistent");
        explain.DiscoveryOutcome.Should().BeNull();
        explain.ReconcileOutcome.Should().BeNull();
    }

    [Fact]
    public void FromPersistedScopeEvaluation_CombinesPersistedScopeWithLivePorts()
    {
        var scope = ReconstructedCurrentScope()
            .RequireSynchronization()
            .MarkSynchronizationInconsistent("material drift");
        var discovery = new LegacyChangeDiscoveryResult(
            LegacyChangeDiscoveryOutcomeEnum.ChangesDetected,
            scope.SynchronizationPosition,
            Array.Empty<LegacyDiscoveredDeltaType>(),
            "fingerprint mismatch");
        var reconcile = new StockReconciliationResult(
            StockReconciliationOutcomeEnum.MaterialInconsistency,
            10m,
            7m,
            3m,
            Array.Empty<StockReconciliationDifferenceType>(),
            "quantity mismatch");

        var explain = StockLedgerSyncExplainability.FromPersistedScopeEvaluation(
            scope,
            discovery,
            reconcile);

        explain.Should().BeEquivalentTo(
            StockLedgerSyncExplainability.FromExecution(scope, discovery, reconcile));
        explain.DiscoveryOutcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.ChangesDetected);
        explain.ReconcileOutcome.Should().Be(StockReconciliationOutcomeEnum.MaterialInconsistency);
        explain.InconsistencyReason.Should().Be("material drift");
    }

    private static StockLedgerScopeStateModel ReconstructedCurrentScope()
        => StockLedgerScopeStateModel.CreateNotReconstructed(Item, ReceiptSource)
            .RequireReconstruction()
            .BeginReconstruction()
            .CompleteReconstruction(
                SynchronizationPositionType.CreateFromUtf8Token("baseline-fp", "fingerprint-v1"),
                reconstructionBasisVersion: "fingerprint-v1");
}
