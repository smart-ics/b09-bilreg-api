using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S2 — Pure sync delta interpreter unit tests (no SQL / I/O).
/// </summary>
public class LegacySyncDeltaInterpreterTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);

    private static readonly IStockLedgerScopeKey Scope =
        StockLedgerScopeKeyType.Create("BRGP3S2A", "DOP3S2A");

    [Fact]
    public void UnchangedDiscovery_ReturnsEmptyIntents()
    {
        var discovery = new LegacyChangeDiscoveryResult(
            LegacyChangeDiscoveryOutcomeEnum.Unchanged,
            CurrentFingerprint: null,
            Array.Empty<LegacyDiscoveredDeltaType>(),
            Explanation: null);

        var result = Interpret(discovery, LegacySyncLedgerSnapshot.Empty, [], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        result.Intents.Should().BeEmpty();
    }

    [Fact]
    public void RequiresScopedReDerive_FailClosed_NoPartialIntents()
    {
        var discovery = new LegacyChangeDiscoveryResult(
            LegacyChangeDiscoveryOutcomeEnum.ChangesDetected,
            CurrentFingerprint: null,
            [
                new LegacyDiscoveredDeltaType(
                    LegacyDiscoveredDeltaKindEnum.JournalInsert,
                    "BK002",
                    "LY01",
                    1m,
                    0m,
                    T2),
                new LegacyDiscoveredDeltaType(
                    LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "Needs re-derive")
            ],
            Explanation: "mixed");

        var result = Interpret(discovery, LegacySyncLedgerSnapshot.Empty, [], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.RequiresScopedReDerive);
        result.Intents.Should().BeEmpty();
        result.Explanation.Should().Contain("re-derive");
    }

    [Fact]
    public void UndeterminableDiscovery_ReturnsAmbiguous()
    {
        var discovery = new LegacyChangeDiscoveryResult(
            LegacyChangeDiscoveryOutcomeEnum.Undeterminable,
            CurrentFingerprint: null,
            Array.Empty<LegacyDiscoveredDeltaType>(),
            "No stored position");

        var result = Interpret(discovery, LegacySyncLedgerSnapshot.Empty, [], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Ambiguous);
        result.Intents.Should().BeEmpty();
    }

    [Fact]
    public void JournalVoidDelete_ProducesReversalIntent()
    {
        var prior = CreatePriorReceipt("MOV-PRIOR-1", "LY01", 1m);
        var snapshot = SnapshotWithJournal("BK002", "LY01", prior);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalVoidDelete,
                "BK002",
                "LY01",
                null,
                null,
                T2,
                "voided"));

        var result = Interpret(discovery, snapshot, [], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var intent = result.Intents.Should().ContainSingle().Subject;
        intent.Kind.Should().Be(LegacySyncIntentKindEnum.ReversePriorMovement);
        intent.TargetMovementId.Should().Be("MOV-PRIOR-1");
        intent.ProposedMovement.Should().NotBeNull();
        intent.ProposedMovement!.MovementKind.Should().Be(StockMovementKindEnum.Reversal);
        intent.ProposedMovement.ReversedMovementId.Should().Be("MOV-PRIOR-1");
        intent.ProposedMovement.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
        prior.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
    }

    [Fact]
    public void JournalVoidDelete_WithoutAnchor_ReturnsAmbiguous()
    {
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalVoidDelete,
                "BK002",
                "LY01",
                null,
                null,
                T2));

        var result = Interpret(discovery, LegacySyncLedgerSnapshot.Empty, [], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Ambiguous);
        result.Intents.Should().BeEmpty();
    }

    [Fact]
    public void JournalUpdate_OutboundDecrease_ProducesCompensatingInbound()
    {
        // Previously represented OUT 10 → legacy now OUT 7 ⇒ compensatory +3 (Inbound).
        var prior = CreatePriorOutbound("MOV-PRIOR-OUT", "LY01", 10m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var updated = Journal("BK001", "LY01", 0m, 7m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalUpdate,
                "BK001",
                "LY01",
                0m,
                7m,
                T2));

        var result = Interpret(discovery, snapshot, [updated], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var intent = result.Intents.Should().ContainSingle().Subject;
        intent.Kind.Should().Be(LegacySyncIntentKindEnum.CorrectPriorMovement);
        intent.TargetMovementId.Should().Be("MOV-PRIOR-OUT");
        intent.ProposedMovement!.MovementKind.Should().Be(StockMovementKindEnum.Correction);
        intent.ProposedMovement.CorrectedMovementId.Should().Be("MOV-PRIOR-OUT");
        intent.ProposedMovement.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
        var line = intent.ProposedMovement.Lines.Should().ContainSingle().Subject;
        line.Direction.Should().Be(StockMovementDirectionEnum.Inbound);
        line.Quantity.Should().Be(3m);
        prior.Lines.Single().Quantity.Should().Be(10m);
    }

    [Fact]
    public void JournalUpdate_InboundDecrease_ProducesCompensatingOutbound()
    {
        // Previously represented IN 100 → legacy now IN 90 ⇒ compensatory -10 (Outbound).
        var prior = CreatePriorReceipt("MOV-PRIOR-IN", "LY01", 100m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var updated = Journal("BK001", "LY01", 90m, 0m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalUpdate,
                "BK001",
                "LY01",
                90m,
                0m,
                T2));

        var result = Interpret(discovery, snapshot, [updated], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var line = result.Intents.Should().ContainSingle().Subject.ProposedMovement!.Lines
            .Should().ContainSingle().Subject;
        line.Direction.Should().Be(StockMovementDirectionEnum.Outbound);
        line.Quantity.Should().Be(10m);
    }

    [Fact]
    public void JournalUpdate_QuantityIncrease_ProducesCompensatingSameDirectionDelta()
    {
        // Previously represented IN 10 → legacy now IN 15 ⇒ compensatory +5 (Inbound).
        var prior = CreatePriorReceipt("MOV-PRIOR-1", "LY01", 10m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var updated = Journal("BK001", "LY01", 15m, 0m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalUpdate,
                "BK001",
                "LY01",
                15m,
                0m,
                T2));

        var result = Interpret(discovery, snapshot, [updated], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var line = result.Intents.Should().ContainSingle().Subject.ProposedMovement!.Lines
            .Should().ContainSingle().Subject;
        line.Direction.Should().Be(StockMovementDirectionEnum.Inbound);
        line.Quantity.Should().Be(5m);
    }

    [Fact]
    public void JournalUpdate_NoQuantityDifference_ProducesNoQuantityConsequence()
    {
        var prior = CreatePriorReceipt("MOV-PRIOR-1", "LY01", 10m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var updated = Journal("BK001", "LY01", 10m, 0m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalUpdate,
                "BK001",
                "LY01",
                10m,
                0m,
                T2));

        var result = Interpret(discovery, snapshot, [updated], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        result.Intents.Should().BeEmpty();
        prior.Lines.Single().Quantity.Should().Be(10m);
    }

    [Fact]
    public void JournalUpdate_UnitCostChange_ReturnsAmbiguous()
    {
        var prior = CreatePriorReceipt("MOV-PRIOR-1", "LY01", 10m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var updated = new LegacyStockJournalEntryType(
            "BK001",
            Scope.BrgId,
            Scope.ReceiptSourceId,
            "LY01",
            QuantityIn: 10m,
            QuantityOut: 0m,
            UnitCost: 2000m,
            ExpA,
            "B1",
            MutationKindId: "DO",
            MutationTransactionId: "BK001",
            T2,
            PurchaseOrderId: null);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalUpdate,
                "BK001",
                "LY01",
                10m,
                0m,
                T2));

        var result = Interpret(discovery, snapshot, [updated], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Ambiguous);
        result.Intents.Should().BeEmpty();
        result.Explanation.Should().Contain("valuation");
    }

    [Fact]
    public void JournalUpdate_DirectionChange_ReturnsAmbiguous()
    {
        var prior = CreatePriorReceipt("MOV-PRIOR-1", "LY01", 10m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var updated = Journal("BK001", "LY01", 0m, 10m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalUpdate,
                "BK001",
                "LY01",
                0m,
                10m,
                T2));

        var result = Interpret(discovery, snapshot, [updated], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Ambiguous);
        result.Intents.Should().BeEmpty();
        result.Explanation.Should().Contain("direction");
    }

    [Fact]
    public void JournalInsert_Inbound_ProducesLegacySynchronizedReceipt()
    {
        var journal = Journal("BK002", "LY01", 5m, 0m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalInsert,
                "BK002",
                "LY01",
                5m,
                0m,
                T2));

        var result = Interpret(discovery, LegacySyncLedgerSnapshot.Empty, [journal], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var intent = result.Intents.Should().ContainSingle().Subject;
        intent.Kind.Should().Be(LegacySyncIntentKindEnum.ApplyLegacySynchronizedReceipt);
        intent.ProposedMovement!.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
        intent.ProposedMovement.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
        intent.SyncIdempotencyKey.Should().StartWith(LegacyChangeDiscoveryIdentityKeys.JournalPrefix);
    }

    [Fact]
    public void JournalInsert_Outbound_ProducesLegacySynchronizedOutbound()
    {
        var journal = Journal("BK003", "LY01", 0m, 2m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalInsert,
                "BK003",
                "LY01",
                0m,
                2m,
                T2));

        var result = Interpret(discovery, LegacySyncLedgerSnapshot.Empty, [journal], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var intent = result.Intents.Should().ContainSingle().Subject;
        intent.Kind.Should().Be(LegacySyncIntentKindEnum.ApplyLegacySynchronizedOutbound);
        intent.ProposedMovement!.MovementKind.Should().Be(StockMovementKindEnum.Outbound);
        intent.ProposedMovement.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
    }

    [Fact]
    public void Repost_ProducesVoidThenInsertIntents()
    {
        var prior = CreatePriorReceipt("MOV-PRIOR-1", "LY01", 10m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var inserted = Journal("BK999", "LY01", 10m, 0m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalVoidDelete,
                "BK001",
                "LY01",
                null,
                null,
                null),
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalInsert,
                "BK999",
                "LY01",
                10m,
                0m,
                T2));

        var result = Interpret(discovery, snapshot, [inserted], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        result.Intents.Should().HaveCount(2);
        result.Intents[0].Kind.Should().Be(LegacySyncIntentKindEnum.ReversePriorMovement);
        result.Intents[0].LegacyJournalId.Should().Be("BK001");
        result.Intents[1].Kind.Should().Be(LegacySyncIntentKindEnum.ApplyLegacySynchronizedReceipt);
        result.Intents[1].LegacyJournalId.Should().Be("BK999");
    }

    [Fact]
    public void DuplicateDeltaIdentity_ProducesSingleIntent()
    {
        var journal = Journal("BK002", "LY01", 1m, 0m, T2);
        var duplicate = new LegacyDiscoveredDeltaType(
            LegacyDiscoveredDeltaKindEnum.JournalInsert,
            "BK002",
            "LY01",
            1m,
            0m,
            T2);
        var discovery = ChangesDetected(duplicate, duplicate);

        var result = Interpret(discovery, LegacySyncLedgerSnapshot.Empty, [journal], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        result.Intents.Should().ContainSingle();
    }

    [Fact]
    public void BalanceUpdate_Decrease_PreservesLayerOrigin()
    {
        var anchorOrigin = StockFactOriginEnum.Reconstructed;
        var snapshot = SnapshotWithBalance(
            "STK001",
            "LY01",
            remaining: 10m,
            origin: anchorOrigin);
        var currentBalance = Balance("LY01", "STK001", 9m);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.BalanceUpdate,
                LegacyJournalId: "STK001",
                "LY01",
                QuantityIn: 9m,
                QuantityOut: null,
                MutationTime: T2));

        var result = Interpret(discovery, snapshot, [], [currentBalance]);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var intent = result.Intents.Should().ContainSingle().Subject;
        intent.Kind.Should().Be(LegacySyncIntentKindEnum.AdjustLayerRemainingQuantity);
        intent.TargetLayerId.Should().Be("LYR-1");
        intent.ProposedMovement!.MovementKind.Should().Be(StockMovementKindEnum.Outbound);
        intent.ProposedMovement.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
        intent.Explanation.Should().Contain(anchorOrigin.ToString());
        snapshot.BalanceAnchors.Values.Single().LayerOrigin.Should().Be(anchorOrigin);
    }

    [Fact]
    public void BalanceDelete_DepletedLayer_RepresentationalOmission()
    {
        var snapshot = SnapshotWithBalance(
            "STK002",
            "LY02",
            remaining: 0m,
            origin: StockFactOriginEnum.Reconstructed);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.BalanceDelete,
                LegacyJournalId: "STK002",
                "LY02",
                null,
                null,
                null));

        var result = Interpret(discovery, snapshot, [], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var intent = result.Intents.Should().ContainSingle().Subject;
        intent.Kind.Should().Be(LegacySyncIntentKindEnum.RepresentationalBalanceOmission);
        intent.ProposedMovement.Should().BeNull();
        intent.TargetLayerId.Should().Be("LYR-1");
        intent.Explanation.Should().Contain("representational");
    }

    [Fact]
    public void BalanceDelete_ActiveLayer_ProducesDepletionIntent()
    {
        var snapshot = SnapshotWithBalance(
            "STK001",
            "LY01",
            remaining: 5m,
            origin: StockFactOriginEnum.Reconstructed);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.BalanceDelete,
                LegacyJournalId: "STK001",
                "LY01",
                null,
                null,
                null));

        var result = Interpret(discovery, snapshot, [], []);

        result.Outcome.Should().Be(LegacySyncInterpretationOutcomeEnum.Succeeded);
        var intent = result.Intents.Should().ContainSingle().Subject;
        intent.Kind.Should().Be(LegacySyncIntentKindEnum.AdjustLayerRemainingQuantity);
        intent.ProposedMovement!.MovementKind.Should().Be(StockMovementKindEnum.Outbound);
        intent.ProposedMovement.Lines.Single().Quantity.Should().Be(5m);
        intent.ProposedMovement.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);
    }

    [Fact]
    public void SameInputs_ProduceSameIntents()
    {
        var prior = CreatePriorReceipt("MOV-PRIOR-1", "LY01", 10m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var inserted = Journal("BK999", "LY01", 10m, 0m, T2);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalVoidDelete,
                "BK001",
                "LY01",
                null,
                null,
                null),
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalInsert,
                "BK999",
                "LY01",
                10m,
                0m,
                T2));

        var first = Interpret(discovery, snapshot, [inserted], []);
        var second = Interpret(discovery, snapshot, [inserted], []);

        first.Should().BeEquivalentTo(second);
    }

    [Fact]
    public void ProposedMovements_DoNotEraseHistory()
    {
        var prior = CreatePriorReceipt("MOV-PRIOR-1", "LY01", 10m);
        var snapshot = SnapshotWithJournal("BK001", "LY01", prior);
        var discovery = ChangesDetected(
            new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalVoidDelete,
                "BK001",
                "LY01",
                null,
                null,
                T2));

        var result = Interpret(discovery, snapshot, [], []);

        var intent = result.Intents.Should().ContainSingle().Subject;
        intent.ProposedMovement!.ReversedMovementId.Should().Be(prior.StockMovementId);
        intent.ProposedMovement.StockMovementId.Should().NotBe(prior.StockMovementId);
        prior.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
        prior.Lines.Should().HaveCount(1);
    }

    private static LegacySyncInterpretationResult Interpret(
        LegacyChangeDiscoveryResult discovery,
        LegacySyncLedgerSnapshot snapshot,
        IReadOnlyList<LegacyStockJournalEntryType> journals,
        IReadOnlyList<LegacyStockBalanceType> balances)
        => LegacySyncDeltaInterpreter.Interpret(Scope, discovery, snapshot, journals, balances);

    private static LegacyChangeDiscoveryResult ChangesDetected(params LegacyDiscoveredDeltaType[] deltas)
        => new(LegacyChangeDiscoveryOutcomeEnum.ChangesDetected, null, deltas, null);

    private static LegacySyncLedgerSnapshot SnapshotWithJournal(
        string journalId,
        string layananId,
        StockMovementModel movement)
    {
        var identity = new LegacyJournalIdentity(layananId, journalId);
        return LegacySyncLedgerSnapshot.Create(
            journalAnchors: new Dictionary<LegacyJournalIdentity, LegacySyncJournalAnchor>
            {
                [identity] = new LegacySyncJournalAnchor(movement.StockMovementId, movement)
            },
            movements: [movement]);
    }

    private static LegacySyncLedgerSnapshot SnapshotWithBalance(
        string legacyRowId,
        string layananId,
        decimal remaining,
        StockFactOriginEnum origin)
    {
        var identity = new LegacyBalanceIdentity(layananId, legacyRowId);
        return LegacySyncLedgerSnapshot.Create(
            balanceAnchors: new Dictionary<LegacyBalanceIdentity, LegacySyncBalanceAnchor>
            {
                [identity] = new LegacySyncBalanceAnchor(
                    "LYR-1",
                    remaining,
                    origin,
                    UnitCost: 1000m,
                    ExpirationDate: ExpA,
                    Batch: "B1")
            });
    }

    private static StockMovementModel CreatePriorReceipt(
        string movementId,
        string layananId,
        decimal quantity)
    {
        var line = StockMovementLineType.Create(
            1,
            BrgObatType.Key(Scope.BrgId),
            ReceiptSourceType.Create(Scope.ReceiptSourceId),
            LayananType.Key(layananId),
            StockMovementDirectionEnum.Inbound,
            quantity,
            UnitValuationType.Create(1000m),
            StockFactOriginEnum.Reconstructed);

        return StockMovementModel.CreateReceipt(
            SourceTransactionReferenceType.Create($"PRIOR|{movementId}"),
            T1,
            [line],
            StockFactOriginEnum.Reconstructed,
            movementId);
    }

    private static StockMovementModel CreatePriorOutbound(
        string movementId,
        string layananId,
        decimal quantity)
    {
        var line = StockMovementLineType.Create(
            1,
            BrgObatType.Key(Scope.BrgId),
            ReceiptSourceType.Create(Scope.ReceiptSourceId),
            LayananType.Key(layananId),
            StockMovementDirectionEnum.Outbound,
            quantity,
            UnitValuationType.Create(1000m),
            StockFactOriginEnum.Reconstructed);

        return StockMovementModel.CreateOutbound(
            SourceTransactionReferenceType.Create($"PRIOR|{movementId}"),
            T1,
            [line],
            StockFactOriginEnum.Reconstructed,
            movementId);
    }

    private static LegacyStockBalanceType Balance(string layananId, string legacyRowId, decimal qty)
        => new(
            Scope.BrgId,
            Scope.ReceiptSourceId,
            layananId,
            qty,
            1000m,
            ExpA,
            "B1",
            PurchaseOrderId: null,
            legacyRowId,
            ReceiptTime: T1,
            LastMutationTime: T2);

    private static LegacyStockJournalEntryType Journal(
        string journalId,
        string layananId,
        decimal qtyIn,
        decimal qtyOut,
        DateTime mutationTime)
        => new(
            journalId,
            Scope.BrgId,
            Scope.ReceiptSourceId,
            layananId,
            qtyIn,
            qtyOut,
            1000m,
            ExpA,
            "B1",
            MutationKindId: "DO",
            MutationTransactionId: journalId,
            mutationTime,
            PurchaseOrderId: null);
}
