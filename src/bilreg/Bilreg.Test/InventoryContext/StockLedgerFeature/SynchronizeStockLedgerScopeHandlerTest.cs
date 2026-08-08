using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.InventoryContext.StockLedgerFeature.Fakes;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S4 — Incremental catch-up orchestration + Synchronization Position advancement.
/// Disposable <c>devTest</c> only; never writes <c>tb_stok</c> / <c>tb_buku</c> via sync.
/// </summary>
[Collection("StockLedgerP3S4")]
public class SynchronizeStockLedgerScopeHandlerTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T3 = new(2024, 2, 1, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);
    private static readonly DateOnly ExpB = new(2025, 6, 1);

    [Fact]
    public async Task HappyPath_LegacyChange_CatchUp_AdvancesPosition_WithLegacySynchronizedMovement()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("HAP");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);

            var bootstrap = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            bootstrap.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);

            // Post-baseline legacy change: inbound insert + balance qty update.
            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 5m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var priorPosition = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;
            var layersBefore = CountLayers(key);

            var result = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);

            // P3-S8 — initial G-24 explainability on happy-path catch-up.
            result.Explainability.Should().NotBeNull();
            result.Explainability!.AlgorithmVersion.Should().Be(
                LegacyReconstructionBasisCalculator.AlgorithmVersion);
            result.Explainability.DiscoveryOutcome.Should().Be(
                LegacyChangeDiscoveryOutcomeEnum.ChangesDetected);
            result.Explainability.ReconcileOutcome.Should().BeOneOf(
                StockReconciliationOutcomeEnum.Balanced,
                StockReconciliationOutcomeEnum.IntentionalDifference,
                StockReconciliationOutcomeEnum.PendingSynchronization);
            result.Explainability.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            result.Explainability.InconsistencyReason.Should().BeNull();

            var expected = LegacyReconstructionBasisCalculator.Compute(changed.Balances, changed.Journals);
            result.SynchronizationPosition.Should().Be(expected);
            result.SynchronizationPosition.Should().NotBe(priorPosition);

            CountLayers(key).Should().BeGreaterThan(layersBefore);
            var movements = ListSyncMovements(key);
            movements.Should().Contain(m => m.Origin == StockFactOriginEnum.LegacySynchronized);

            var newLayer = harness.Repos.Position
                .ListByLedgerScope(key)
                .SelectMany(p => p.Layers)
                .First(l => l.Origin == StockFactOriginEnum.LegacySynchronized);
            newLayer.Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);

            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task FirstSynchronization_BootstrapsDiscoveryIdentityKeys_R001()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("B01");
        Cleanup(key);

        try
        {
            var baseline = MultiLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);

            CountSyncIdentityKeys(key).Should().Be(0);

            var result = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);
            CountSyncIdentityKeys(key).Should().Be(
                baseline.Journals.Count + baseline.Balances.Count);

            // Second call with unchanged legacy is AlreadyCurrent.
            var second = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            second.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task MaxLengthDiscoveryIdentityKey_PersistsSuccessfully_R002()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("LEN");
        Cleanup(key);

        try
        {
            // Schema-max field widths produce ~180 chars; pad past former VARCHAR(200) limit
            // to prove R-002 VARCHAR(400) capacity while staying within the new bound.
            var journal = new LegacyStockJournalEntryType(
                LegacyJournalId: new string('J', 10),
                key.BrgId,
                key.ReceiptSourceId,
                LayananId: new string('L', 5),
                QuantityIn: 9999999999999999.99m,
                QuantityOut: 0m,
                UnitCost: 9999999999999999.99m,
                ExpirationDate: new DateOnly(2099, 12, 31),
                Batch: new string('B', 15),
                MutationKindId: new string('M', 10),
                MutationTransactionId: new string('T', 10),
                MutationTime: new DateTime(2099, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified),
                PurchaseOrderId: new string('P', 10));

            var baseKey = LegacyChangeDiscoveryIdentityKeys.BuildJournalKey(key, journal);
            var syncKey = baseKey + "|" + new string('X', Math.Max(0, 350 - baseKey.Length));
            syncKey.Length.Should().BeGreaterThan(200);
            syncKey.Length.Should().BeLessThanOrEqualTo(400);

            var harness = CreateHarness(SingleLocationSnapshot(key));
            var commit = harness.Uow.CommitSyncEvidence(new StockSyncEvidenceDraft(
                IdempotencyKey: syncKey,
                ProcessedAt: T1,
                BrgId: key.BrgId,
                ReceiptSourceId: key.ReceiptSourceId));

            commit.Outcome.Should().Be(StockConsequenceCommitOutcomeEnum.Committed);
            var loaded = harness.Repos.Idempotency.LoadByBusinessKey(
                StockSourceIdempotencyModel.BusinessKey(
                    StockSourceIdempotencyKindEnum.SyncBatch,
                    syncKey));
            loaded.HasValue.Should().BeTrue();
            loaded.Value.IdempotencyKey.Should().Be(syncKey);
            loaded.Value.IdempotencyKey.Length.Should().Be(syncKey.Length);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task SyntheticVoidAndOmissionKeys_PersistOpaque_WithoutTryParse_R004()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("OPQ");
        Cleanup(key);

        try
        {
            var voidKey = string.Join('|', "SYNC", "BUKU", key.BrgId, key.ReceiptSourceId, "LY01", "TRS001", "VOID", "MOV123");
            var omissionKey = string.Join('|', "SYNC", "STOK", key.BrgId, key.ReceiptSourceId, "LY01", "STK001", "0", "OMISSION");

            LegacyChangeDiscoveryIdentityKeys.TryParseJournalKey(voidKey, out _, out _).Should().BeFalse();
            LegacyChangeDiscoveryIdentityKeys.TryParseBalanceKey(omissionKey, out _, out _).Should().BeFalse();

            var harness = CreateHarness(SingleLocationSnapshot(key));
            harness.Uow.CommitSyncEvidence(new StockSyncEvidenceDraft(
                voidKey, T1, key.BrgId, key.ReceiptSourceId)).Outcome
                .Should().Be(StockConsequenceCommitOutcomeEnum.Committed);
            harness.Uow.CommitSyncEvidence(new StockSyncEvidenceDraft(
                omissionKey, T1, key.BrgId, key.ReceiptSourceId)).Outcome
                .Should().Be(StockConsequenceCommitOutcomeEnum.Committed);

            harness.Repos.Idempotency.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(StockSourceIdempotencyKindEnum.SyncBatch, voidKey))
                .HasValue.Should().BeTrue();
            harness.Repos.Idempotency.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(StockSourceIdempotencyKindEnum.SyncBatch, omissionKey))
                .HasValue.Should().BeTrue();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task DuplicateSyncBatch_IsQuantityNeutral()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("DUP");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 3m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var first = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            first.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);

            var layersAfterFirst = CountLayers(key);
            var qtyAfterFirst = TotalRemaining(key);
            var positionAfterFirst = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition;

            // Replay same legacy snapshot — Unchanged / AlreadyCurrent or quantity-neutral.
            var second = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            second.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent);

            CountLayers(key).Should().Be(layersAfterFirst);
            TotalRemaining(key).Should().Be(qtyAfterFirst);
            harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition.Should().Be(positionAfterFirst);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task JournalVoid_RetainsHistory_AndCreatesReversal()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("VOI");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var priorPosition = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;

            // Void the establishing inbound journal: remove journal and balance (depleted intentional path).
            var voided = new Snapshot(
                Array.Empty<LegacyStockBalanceType>(),
                Array.Empty<LegacyStockJournalEntryType>());
            harness.LegacyRead.Balances = voided.Balances;
            harness.LegacyRead.JournalEntries = voided.Journals;

            var reconMovementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key);
            harness.Repos.Movement.LoadEntity(StockMovementModel.Key(reconMovementId))
                .HasValue.Should().BeTrue();

            var result = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            // Safe 1:1 single-location void must succeed (R-007 happy path).
            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            result.SynchronizationPosition.Should().NotBeNull();
            result.SynchronizationPosition.Should().NotBe(priorPosition);

            harness.Repos.Movement.LoadEntity(StockMovementModel.Key(reconMovementId))
                .HasValue.Should().BeTrue("Original reconstruction movement must be retained.");

            var reversals = ListSyncMovements(key)
                .Where(m => m.MovementKind == StockMovementKindEnum.Reversal)
                .ToList();
            reversals.Should().ContainSingle("Exactly one accountable reversal Movement is expected.");
            reversals[0].ReversedMovementId.Should().Be(reconMovementId);
            reversals[0].Origin.Should().Be(StockFactOriginEnum.LegacySynchronized);

            var ly01 = harness.Repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY01"))
                .Value;
            ly01.Layers.Sum(l => l.RemainingQuantity).Should().Be(0m);

            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task JournalVoid_MultiLocationBaseline_FailsClosed_SiblingQuantityUnchanged_R007()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("V07");
        Cleanup(key);

        try
        {
            var baseline = MultiLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var priorPosition = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;
            var ly02Before = harness.Repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY02"))
                .Value;
            var ly02RemainingBefore = ly02Before.Layers.Sum(l => l.RemainingQuantity);
            ly02RemainingBefore.Should().Be(7m);

            // Void only the LY01 journal; keep LY02 journal + balance (and LY01 balance unchanged).
            var partialVoid = new Snapshot(
                baseline.Balances.ToArray(),
                baseline.Journals.Where(j => j.LayananId != "LY01").ToArray());
            harness.LegacyRead.Balances = partialVoid.Balances;
            harness.LegacyRead.JournalEntries = partialVoid.Journals;

            var reconMovementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key);
            harness.Repos.Movement.LoadEntity(StockMovementModel.Key(reconMovementId))
                .HasValue.Should().BeTrue();

            var result = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(
                SynchronizeStockLedgerScopeOutcomeEnum.Inconsistent,
                "Aggregate multi-location prior must fail closed before any over-broad Reverse (R-007).");
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Inconsistent);
            harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition.Should().Be(priorPosition);

            harness.Repos.Movement.LoadEntity(StockMovementModel.Key(reconMovementId))
                .HasValue.Should().BeTrue("Original reconstruction movement must be retained.");

            ListSyncMovements(key)
                .Where(m => m.MovementKind == StockMovementKindEnum.Reversal)
                .Should().BeEmpty("No full multi-line Reverse may be persisted for an unsafe void target.");

            var ly02After = harness.Repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY02"))
                .Value;
            ly02After.Layers.Sum(l => l.RemainingQuantity).Should().Be(
                ly02RemainingBefore,
                "Sibling location Remaining Quantity must be unchanged when void fails closed.");

            var ly01After = harness.Repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY01"))
                .Value;
            ly01After.Layers.Sum(l => l.RemainingQuantity).Should().Be(10m);

            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task BalanceUpdate_PreservesLayerEstablishmentOrigin_R003()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("ORG");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var layerBefore = harness.Repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY01"))
                .Value.Layers.Single();
            layerBefore.Origin.Should().Be(StockFactOriginEnum.Reconstructed);

            // Quantity-only balance decrease (no new journal) → BalanceUpdate only.
            var reduced = new Snapshot(
                [Balance(key, "LY01", 7m, 1000m, ExpA, "STK001", T1, "B1")],
                [Journal(key, "TRS001", "LY01", 10m, 0m, 1000m, ExpA, T1, "B1")]);
            harness.LegacyRead.Balances = reduced.Balances;
            harness.LegacyRead.JournalEntries = reduced.Journals;

            var result = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);

            var layerAfter = harness.Repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY01"))
                .Value.Layers.Single(l => l.StockLayerId == layerBefore.StockLayerId);
            layerAfter.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
            layerAfter.RemainingQuantity.Should().Be(7m);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task MaterialInconsistency_DoesNotAdvancePosition()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("MIS");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var prior = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 4m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var syncThrow = CreateSyncHandler(harness.LegacyRead, throwingReconcile: true);
            var result = await syncThrow.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Inconsistent);
            harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition.Should().Be(prior);

            // P3-S8 — initial G-24 explainability on Inconsistent path.
            result.Explainability.Should().NotBeNull();
            result.Explainability!.AlgorithmVersion.Should().Be(
                LegacyReconstructionBasisCalculator.AlgorithmVersion);
            result.Explainability.DiscoveryOutcome.Should().Be(
                LegacyChangeDiscoveryOutcomeEnum.ChangesDetected);
            result.Explainability.ReconcileOutcome.Should().Be(
                StockReconciliationOutcomeEnum.MaterialInconsistency);
            result.Explainability.SynchronizationState.Should().Be(SynchronizationStateEnum.Inconsistent);
            result.Explainability.InconsistencyReason.Should().NotBeNullOrWhiteSpace();
            result.ScopeState.InconsistencyReason.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task ProvenanceLimitation_DoesNotAdvance_WhenNotReconstructed()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("PRV");
        Cleanup(key);

        try
        {
            var harness = CreateHarness(SingleLocationSnapshot(key));
            // No reconstruction — Scope missing.
            var act = () => harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*was not found*");
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task CrashBeforeFinalize_LeavesPriorPositionUnchanged()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("CRH");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            var boot = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            boot.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);
            var prior = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 2m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            // Force reconcile to fail closed by using a throwing reconciliation port wrapper.
            var throwing = CreateHarness(changed, useThrowingReconcile: true);
            // Copy reconstructed state is already in DB; rebuild sync handler with throwing reconcile.
            var syncThrow = CreateSyncHandler(harness.LegacyRead, throwingReconcile: true);

            var result = await syncThrow.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Inconsistent);
            harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition.Should().Be(prior);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task RequiresScopedReDerive_WithoutKeys_Bootstraps_WhenMaterialMatches()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("RDR");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);

            // Force fingerprint mismatch without keys by mutating stored position via SQL.
            using (var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value)))
            {
                conn.Open();
                conn.Execute(
                    """
                    UPDATE BILRG_StokLedgerScope
                    SET SynchronizationPositionOpaque = 0xDEADBEEF
                    WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
                    """,
                    new { key.BrgId, key.ReceiptSourceId });
            }

            CountSyncIdentityKeys(key).Should().Be(0);

            var result = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);
            CountSyncIdentityKeys(key).Should().BeGreaterThan(0);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task PendingSynchronization_MaterialSafe_CompletesSynchronization_R006()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("PND");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);

            // First sync: bootstrap while claiming SynchronizationRequired; reconcile returns
            // PendingSynchronization overlay; material-safe advance still completes.
            var result = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            result.SynchronizationPosition.Should().NotBeNull();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task Synchronization_DoesNotModifyLegacyAuthorityRows()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var key = NewScopeKey("LEG");
        Cleanup(key);

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var stokBefore = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_stok");
        var bukuBefore = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_buku");

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_stok").Should().Be(stokBefore);
            conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_buku").Should().Be(bukuBefore);
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    private static async Task ReconstructAsync(Harness harness, IStockLedgerScopeKey key)
    {
        var result = await harness.Reconstruct.Handle(
            new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
        result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);
    }

    private static Harness CreateHarness(Snapshot snapshot, bool useThrowingReconcile = false)
    {
        var fakeRead = new FakeLegacyStockReadPort
        {
            Balances = snapshot.Balances,
            JournalEntries = snapshot.Journals
        };
        return CreateHarness(fakeRead, useThrowingReconcile);
    }

    private static Harness CreateHarness(FakeLegacyStockReadPort fakeRead, bool useThrowingReconcile = false)
    {
        var options = ConnStringHelper.GetTestEnv();
        var spy = new SpyUnitOfWork();
        var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
        var movementRepo = new StockMovementRepo(
            new StockMovementDal(options),
            new StockMovementLineDal(options));
        var positionRepo = new StockPositionRepo(
            new StockPositionDal(options),
            new StockLayerDal(options));
        var idempotencyDal = new StockSourceIdempotencyDal(options);
        var idempotencyRepo = new StockSourceIdempotencyRepo(idempotencyDal);
        var legacyWriter = new FakeLegacyCompatibilityWriterPort();
        var uow = new StockConsequenceUnitOfWork(
            spy, idempotencyRepo, movementRepo, positionRepo, scopeRepo, legacyWriter);

        var claim = new ReconstructionClaimService(spy, scopeRepo);
        var reconstruct = new ReconstructStockLedgerBaselineHandler(
            claim, fakeRead, uow, spy, scopeRepo, idempotencyRepo);

        var discovery = new LegacyChangeDiscoveryPort(fakeRead, idempotencyDal);
        IStockReconciliationPort reconcile = useThrowingReconcile
            ? new MaterialInconsistencyReconciliationPort()
            : new StockReconciliationPort(fakeRead, new StockLayerDal(options), scopeRepo);

        var snapshotLoader = new LegacySyncLedgerSnapshotLoader(
            idempotencyRepo, positionRepo, movementRepo);
        var bootstrapper = new LegacySyncIdentityBootstrapper(
            uow, idempotencyRepo, movementRepo);
        var syncClaim = new SynchronizationClaimService(spy, scopeRepo);

        var sync = new SynchronizeStockLedgerScopeHandler(
            scopeRepo,
            discovery,
            fakeRead,
            reconcile,
            uow,
            spy,
            syncClaim,
            snapshotLoader,
            bootstrapper,
            positionRepo,
            idempotencyRepo);

        return new Harness(
            reconstruct,
            sync,
            fakeRead,
            legacyWriter,
            uow,
            new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo));
    }

    private static SynchronizeStockLedgerScopeHandler CreateSyncHandler(
        FakeLegacyStockReadPort fakeRead,
        bool throwingReconcile)
    {
        return CreateHarness(fakeRead, throwingReconcile).Sync;
    }

    private static Snapshot SingleLocationSnapshot(IStockLedgerScopeKey key)
        => new(
            [Balance(key, "LY01", 10m, 1000m, ExpA, "STK001", T1, "B1")],
            [Journal(key, "TRS001", "LY01", 10m, 0m, 1000m, ExpA, T1, "B1")]);

    private static Snapshot MultiLocationSnapshot(IStockLedgerScopeKey key)
        => new(
            [
                Balance(key, "LY01", 10m, 1000m, ExpA, "STK001", T1, "B1"),
                Balance(key, "LY02", 7m, 1500m, ExpB, "STK002", T2, "B2")
            ],
            [
                Journal(key, "TRS001", "LY01", 10m, 0m, 1000m, ExpA, T1, "B1"),
                Journal(key, "TRS002", "LY02", 10m, 3m, 1500m, ExpB, T2, "B2")
            ]);

    private static Snapshot ChangedWithJournalInsert(
        IStockLedgerScopeKey key,
        Snapshot baseline,
        decimal qtyIn)
    {
        var balances = baseline.Balances
            .Select(b => b.LayananId == "LY01"
                ? b with { Quantity = b.Quantity + qtyIn }
                : b)
            .ToArray();
        var journals = baseline.Journals
            .Concat([
                Journal(key, "TRS-NEW", "LY01", qtyIn, 0m, 1000m, ExpA, T3, "B1")
            ])
            .ToArray();
        return new Snapshot(balances, journals);
    }

    private static LegacyStockBalanceType Balance(
        IStockLedgerScopeKey scope,
        string layananId,
        decimal qty,
        decimal unitCost,
        DateOnly exp,
        string legacyRowId,
        DateTime receiptTime,
        string? batch)
        => new(
            scope.BrgId,
            scope.ReceiptSourceId,
            layananId,
            qty,
            unitCost,
            exp,
            batch,
            PurchaseOrderId: null,
            legacyRowId,
            receiptTime,
            LastMutationTime: receiptTime);

    private static LegacyStockJournalEntryType Journal(
        IStockLedgerScopeKey scope,
        string journalId,
        string layananId,
        decimal qtyIn,
        decimal qtyOut,
        decimal unitCost,
        DateOnly exp,
        DateTime mutationTime,
        string? batch)
        => new(
            journalId,
            scope.BrgId,
            scope.ReceiptSourceId,
            layananId,
            qtyIn,
            qtyOut,
            unitCost,
            exp,
            batch,
            MutationKindId: "DO",
            MutationTransactionId: journalId,
            mutationTime,
            PurchaseOrderId: null);

    private static StockLedgerScopeKeyType NewScopeKey(string tag)
    {
        // Use full ULID slices so parallel tests cannot collide on truncated prefixes.
        var ulid = Ulid.NewUlid().ToString();
        var tag3 = tag.Length <= 3 ? tag : tag[..3];
        return StockLedgerScopeKeyType.Create(
            (tag3 + ulid)[..13],
            (tag3 + ulid)[3..13]);
    }

    private static void Cleanup(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var reconMovementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key);
        conn.Execute(
            """
            DELETE FROM BILRG_StokMovementLine
            WHERE StockMovementId IN (
                SELECT StockMovementId FROM BILRG_StokSourceIdempotency
                WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId AND StockMovementId <> ''
                UNION
                SELECT LayerFormingMovementId FROM BILRG_StokLayer
                WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
                UNION
                SELECT @ReconMovementId);
            DELETE FROM BILRG_StokMovement
            WHERE StockMovementId IN (
                SELECT StockMovementId FROM BILRG_StokSourceIdempotency
                WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId AND StockMovementId <> ''
                UNION
                SELECT LayerFormingMovementId FROM BILRG_StokLayer
                WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
                UNION
                SELECT @ReconMovementId);
            DELETE FROM BILRG_StokLayer WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokPosition WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokLedgerScope WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokSourceIdempotency
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            """,
            new
            {
                key.BrgId,
                key.ReceiptSourceId,
                ReconMovementId = reconMovementId
            });
    }

    private static int CountLayers(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_StokLayer WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId",
            new { key.BrgId, key.ReceiptSourceId });
    }

    private static int CountSyncIdentityKeys(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokSourceIdempotency
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
              AND IdempotencyKey LIKE 'SYNC|%'
            """,
            new { key.BrgId, key.ReceiptSourceId });
    }

    private static decimal TotalRemaining(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<decimal>(
            """
            SELECT ISNULL(SUM(RemainingQuantity), 0)
            FROM BILRG_StokLayer
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
            """,
            new { key.BrgId, key.ReceiptSourceId });
    }

    private static IReadOnlyList<StockMovementModel> ListSyncMovements(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var ids = conn.Query<string>(
            """
            SELECT DISTINCT StockMovementId FROM BILRG_StokSourceIdempotency
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
              AND IdempotencyKind = @Kind
              AND StockMovementId <> ''
            """,
            new
            {
                key.BrgId,
                key.ReceiptSourceId,
                Kind = (int)StockSourceIdempotencyKindEnum.SyncBatch
            }).ToList();

        var options = ConnStringHelper.GetTestEnv();
        var repo = new StockMovementRepo(new StockMovementDal(options), new StockMovementLineDal(options));
        return ids
            .Select(id => repo.LoadEntity(StockMovementModel.Key(id)))
            .Where(m => m.HasValue)
            .Select(m => m.Value)
            .ToList();
    }

    private sealed record Snapshot(
        IReadOnlyList<LegacyStockBalanceType> Balances,
        IReadOnlyList<LegacyStockJournalEntryType> Journals);

    private sealed record Repos(
        IStockLedgerScopeStateRepo Scope,
        IStockMovementRepo Movement,
        IStockPositionRepo Position,
        IStockSourceIdempotencyRepo Idempotency);

    private sealed record Harness(
        ReconstructStockLedgerBaselineHandler Reconstruct,
        SynchronizeStockLedgerScopeHandler Sync,
        FakeLegacyStockReadPort LegacyRead,
        FakeLegacyCompatibilityWriterPort LegacyWriter,
        IStockConsequenceUnitOfWork Uow,
        Repos Repos);

    private sealed class MaterialInconsistencyReconciliationPort : IStockReconciliationPort
    {
        public StockReconciliationResult Reconcile(IStockLedgerScopeKey scope)
            => new(
                StockReconciliationOutcomeEnum.MaterialInconsistency,
                0m,
                99m,
                99m,
                Array.Empty<StockReconciliationDifferenceType>(),
                "Forced material inconsistency for crash-before-finalize test.");
    }
}
