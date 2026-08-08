using System.Reflection;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockLedgerScopeStateTest
{
    private static readonly BrgReff Item = new("BRG01", "Paracetamol");
    private static readonly ReceiptSourceType ReceiptSource = ReceiptSourceType.Create("DO001");

    private static SynchronizationPositionType Position(string token, string algorithm = "fingerprint-v1")
        => SynchronizationPositionType.CreateFromUtf8Token(token, algorithm);

    private static StockLedgerScopeStateModel NewScope()
        => StockLedgerScopeStateModel.CreateNotReconstructed(Item, ReceiptSource);

    private static StockLedgerScopeStateModel ReconstructedScope(
        string positionToken = "baseline-fp-1",
        string? basisVersion = "basis-1")
        => NewScope()
            .RequireReconstruction()
            .BeginReconstruction()
            .CompleteReconstruction(Position(positionToken), basisVersion);

    [Fact]
    public void UT01_CreateNotReconstructed_HasExpectedInitialState()
    {
        var state = NewScope();

        state.BrgId.Should().Be("BRG01");
        state.ReceiptSourceId.Should().Be("DO001");
        state.ScopeKey.Should().Be(StockLedgerScopeKeyType.Create("BRG01", "DO001"));
        state.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.NotReconstructed);
        state.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        state.SynchronizationPosition.Should().BeNull();
        state.HasSynchronizationPosition.Should().BeFalse();
        state.ReconstructionBasisVersion.Should().BeNull();
        state.InconsistencyReason.Should().BeNull();
    }

    [Fact]
    public void UT02_RequireReconstruction_FromNotReconstructed_Succeeds()
    {
        var next = NewScope().RequireReconstruction();

        next.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.ReconstructionRequired);
        next.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        next.SynchronizationPosition.Should().BeNull();
    }

    [Fact]
    public void UT03_ReconstructionHappyPath_InitializesOpaquePosition()
    {
        var position = Position("fp-abc", "fingerprint-v1");
        var state = NewScope()
            .RequireReconstruction()
            .BeginReconstruction()
            .CompleteReconstruction(position, "basis-v2");

        state.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
        state.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        state.SynchronizationPosition.Should().Be(position);
        state.SynchronizationPosition!.AlgorithmVersion.Should().Be("fingerprint-v1");
        state.ReconstructionBasisVersion.Should().Be("basis-v2");
        state.InconsistencyReason.Should().BeNull();
    }

    [Fact]
    public void UT04_MarkReconstructionInconsistent_FromReconstructing_Succeeds()
    {
        var state = NewScope()
            .RequireReconstruction()
            .BeginReconstruction()
            .MarkReconstructionInconsistent("Receipt Source distribution undetermined");

        state.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Inconsistent);
        state.SynchronizationState.Should().Be(SynchronizationStateEnum.Inconsistent);
        state.SynchronizationPosition.Should().BeNull();
        state.InconsistencyReason.Should().Be("Receipt Source distribution undetermined");
    }

    [Fact]
    public void UT05_IllegalReconstructionTransitions_AreRejected()
    {
        var notReconstructed = NewScope();
        var required = notReconstructed.RequireReconstruction();
        var reconstructing = required.BeginReconstruction();
        var reconstructed = reconstructing.CompleteReconstruction(Position("fp-1"));

        Action beginFromStart = () => notReconstructed.BeginReconstruction();
        Action completeFromRequired = () => required.CompleteReconstruction(Position("fp-x"));
        Action requireAgain = () => reconstructed.RequireReconstruction();
        Action beginFromReconstructed = () => reconstructed.BeginReconstruction();

        beginFromStart.Should().Throw<InvalidOperationException>();
        completeFromRequired.Should().Throw<InvalidOperationException>();
        requireAgain.Should().Throw<InvalidOperationException>();
        beginFromReconstructed.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UT06_SynchronizationLifecycle_AdvancesOpaquePosition()
    {
        var baseline = Position("fp-1");
        var advanced = Position("fp-2");

        var state = ReconstructedScope("fp-1")
            .MarkLegacyChangePending()
            .RequireSynchronization()
            .CompleteSynchronization(advanced);

        state.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
        state.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        state.SynchronizationPosition.Should().Be(advanced);
        state.SynchronizationPosition.Should().NotBe(baseline);
    }

    [Fact]
    public void UT07_RequireSynchronization_FromCurrent_IsAllowed()
    {
        var state = ReconstructedScope().RequireSynchronization();

        state.SynchronizationState.Should().Be(SynchronizationStateEnum.SynchronizationRequired);
        state.HasSynchronizationPosition.Should().BeTrue();
    }

    [Fact]
    public void UT08_MarkSynchronizationInconsistent_KeepsReconstructedStatus()
    {
        var state = ReconstructedScope()
            .RequireSynchronization()
            .MarkSynchronizationInconsistent("Material quantity drift vs legacy authority");

        state.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
        state.SynchronizationState.Should().Be(SynchronizationStateEnum.Inconsistent);
        state.InconsistencyReason.Should().Be("Material quantity drift vs legacy authority");
        state.HasSynchronizationPosition.Should().BeTrue();
    }

    [Fact]
    public void UT09_IllegalSynchronizationTransitions_AreRejected()
    {
        var notReconstructed = NewScope();
        var reconstructed = ReconstructedScope();
        var pending = reconstructed.MarkLegacyChangePending();
        var required = pending.RequireSynchronization();
        var inconsistent = required.MarkSynchronizationInconsistent("drift");

        Action syncBeforeBaseline = () => notReconstructed.RequireSynchronization();
        Action completeFromCurrent = () => reconstructed.CompleteSynchronization(Position("fp-x"));
        Action pendingFromRequired = () => required.MarkLegacyChangePending();
        Action syncFromInconsistent = () => inconsistent.RequireSynchronization();

        syncBeforeBaseline.Should().Throw<InvalidOperationException>();
        completeFromCurrent.Should().Throw<InvalidOperationException>();
        pendingFromRequired.Should().Throw<InvalidOperationException>();
        syncFromInconsistent.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UT10_OpaquePosition_RoundTripsInMemory_WithDefensiveCopy()
    {
        var bytes = new byte[] { 0x01, 0x02, 0xAA, 0xFF };
        var original = SynchronizationPositionType.Create(bytes, "fingerprint-v1");
        bytes[0] = 0x99;

        original.OpaqueValue.Should().Equal(new byte[] { 0x01, 0x02, 0xAA, 0xFF });
        original.AlgorithmVersion.Should().Be("fingerprint-v1");

        var sameContent = SynchronizationPositionType.Create(
            new byte[] { 0x01, 0x02, 0xAA, 0xFF },
            "fingerprint-v1");
        var differentAlgo = SynchronizationPositionType.Create(
            new byte[] { 0x01, 0x02, 0xAA, 0xFF },
            "fingerprint-v2");
        var differentBytes = SynchronizationPositionType.Create(
            new byte[] { 0x01, 0x02, 0xAA, 0x00 },
            "fingerprint-v1");

        original.Should().Be(sameContent);
        original.Should().NotBe(differentAlgo);
        original.Should().NotBe(differentBytes);
        original.GetHashCode().Should().Be(sameContent.GetHashCode());

        Action empty = () => SynchronizationPositionType.Create(Array.Empty<byte>(), "fingerprint-v1");
        Action blankAlgo = () => SynchronizationPositionType.Create(new byte[] { 1 }, " ");
        empty.Should().Throw<ArgumentException>();
        blankAlgo.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UT11_OpaquePosition_HasNoWatermarkCursorFields()
    {
        var names = typeof(SynchronizationPositionType)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(m => m.Name)
            .ToArray();

        names.Should().NotContain(n =>
            n.Contains("TglJam", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Timestamp", StringComparison.OrdinalIgnoreCase)
            || n.Contains("KdTrs", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Watermark", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Cursor", StringComparison.OrdinalIgnoreCase));

        names.Should().Contain("OpaqueValue");
        names.Should().Contain("AlgorithmVersion");
    }

    [Fact]
    public void UT12_OriginEnum_IsOrthogonalToScopeSynchronizationState()
    {
        var scope = ReconstructedScope();
        var origins = Enum.GetValues<StockFactOriginEnum>();

        foreach (var origin in origins)
        {
            // Scope state has no Origin property; fact origin must not alter sync state.
            scope.GetType().GetProperty("Origin").Should().BeNull();
            scope.GetType().GetProperty("StockFactOrigin").Should().BeNull();
            scope.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);

            // Changing origin classification of a fact/layer is outside this aggregate.
            origin.Should().BeOneOf(
                StockFactOriginEnum.Native,
                StockFactOriginEnum.Reconstructed,
                StockFactOriginEnum.LegacySynchronized);
        }

        var afterSyncMark = scope.RequireSynchronization();
        afterSyncMark.SynchronizationState.Should().Be(SynchronizationStateEnum.SynchronizationRequired);
        // Origin set remains independently usable regardless of sync state transitions.
        Enum.GetNames<StockFactOriginEnum>().Should().BeEquivalentTo(
            "Native", "Reconstructed", "LegacySynchronized");
    }

    [Fact]
    public void UT13_ScopeState_HasNoAuthoritySemantics()
    {
        var types = new[]
        {
            typeof(StockLedgerScopeStateModel),
            typeof(SynchronizationPositionType)
        };

        foreach (var type in types)
        {
            type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name)
                .Should()
                .NotContain(name => name.Contains("Authoritative", StringComparison.OrdinalIgnoreCase),
                    because: $"{type.Name} must not encode authority");
        }
    }

    [Fact]
    public void UT14_Transitions_DoNotMutateOriginalInstance()
    {
        var original = NewScope();
        var required = original.RequireReconstruction();

        original.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.NotReconstructed);
        required.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.ReconstructionRequired);
        ReferenceEquals(original, required).Should().BeFalse();
    }

    [Fact]
    public void UT15_EstablishFromNativeReceipt_FromNotReconstructed_Succeeds()
    {
        var position = Position("native-fp-1", "fingerprint-v1");
        var state = NewScope().EstablishFromNativeReceipt(position, "fingerprint-v1");

        state.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
        state.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        state.SynchronizationPosition.Should().Be(position);
        state.ReconstructionBasisVersion.Should().Be("fingerprint-v1");
        state.InconsistencyReason.Should().BeNull();
    }

    [Fact]
    public void UT16_EstablishFromNativeReceipt_IllegalSources_AreRejected()
    {
        var notReconstructed = NewScope();
        var required = notReconstructed.RequireReconstruction();
        var reconstructing = required.BeginReconstruction();
        var reconstructed = reconstructing.CompleteReconstruction(Position("fp-1"));
        var inconsistent = NewScope()
            .RequireReconstruction()
            .BeginReconstruction()
            .MarkReconstructionInconsistent("ambiguous");

        Action fromRequired = () => required.EstablishFromNativeReceipt(Position("fp-x"));
        Action fromReconstructing = () => reconstructing.EstablishFromNativeReceipt(Position("fp-x"));
        Action fromReconstructed = () => reconstructed.EstablishFromNativeReceipt(Position("fp-x"));
        Action fromInconsistent = () => inconsistent.EstablishFromNativeReceipt(Position("fp-x"));
        Action nullPosition = () => notReconstructed.EstablishFromNativeReceipt(null!);

        fromRequired.Should().Throw<InvalidOperationException>();
        fromReconstructing.Should().Throw<InvalidOperationException>();
        fromReconstructed.Should().Throw<InvalidOperationException>();
        fromInconsistent.Should().Throw<InvalidOperationException>();
        nullPosition.Should().Throw<ArgumentNullException>();
    }
}
