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
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S5 / G-12 — Legacy Freshness Gate.
/// Disposable <c>devTest</c> for integration paths; unit fakes for undeterminable / already-Inconsistent.
/// </summary>
[Collection("StockLedgerP3S4")]
public class LegacyStockFreshnessGateTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T3 = new(2024, 2, 1, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);

    [Fact]
    public async Task UnchangedScope_AfterBootstrap_ReturnsCurrent_WithoutSecondSync()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("CUR");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var syncCalls = 0;
            var gate = CreateGate(harness, async (cmd, ct) =>
            {
                syncCalls++;
                return await harness.Sync.Handle(cmd, ct);
            });

            var result = await gate.EnsureFreshAsync(key);

            result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.Current);
            result.IsSafeToTrustLedgerLayers.Should().BeTrue();
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            syncCalls.Should().Be(0, "Unchanged + coverage complete must not invoke catch-up.");
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task LegacyChange_TriggersSingleCatchUp_ReturnsSynchronizedNow()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("SYN");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var priorPosition = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;
            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 5m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var syncCalls = 0;
            var gate = CreateGate(harness, async (cmd, ct) =>
            {
                syncCalls++;
                return await harness.Sync.Handle(cmd, ct);
            });

            var result = await gate.EnsureFreshAsync(key);

            result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.SynchronizedNow);
            result.IsSafeToTrustLedgerLayers.Should().BeTrue();
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            result.SynchronizationPosition.Should().NotBe(priorPosition);
            syncCalls.Should().Be(1, "Gate must invoke catch-up at most once per call.");
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task AfterSuccessfulSync_SecondGateCall_ReturnsCurrent()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("2ND");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 3m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var syncCalls = 0;
            var gate = CreateGate(harness, async (cmd, ct) =>
            {
                syncCalls++;
                return await harness.Sync.Handle(cmd, ct);
            });

            var first = await gate.EnsureFreshAsync(key);
            first.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.SynchronizedNow);

            var second = await gate.EnsureFreshAsync(key);
            second.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.Current);
            second.IsSafeToTrustLedgerLayers.Should().BeTrue();
            syncCalls.Should().Be(1, "Second gate call on unchanged scope must not re-sync.");
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task UndeterminableDiscovery_ReturnsStaleOrNotCurrent_WithoutSync()
    {
        var key = StockLedgerScopeKeyType.Create("UT-UNDET-BRG", "UT-UNDET-RS");
        var position = SynchronizationPositionType.CreateFromUtf8Token(
            "unit-fingerprint",
            LegacyReconstructionBasisCalculator.AlgorithmVersion);
        var scope = StockLedgerScopeStateModel.Rehydrate(
            key.BrgId,
            key.ReceiptSourceId,
            ReconstructionStatusEnum.Reconstructed,
            SynchronizationStateEnum.Current,
            position,
            reconstructionBasisVersion: LegacyReconstructionBasisCalculator.AlgorithmVersion,
            inconsistencyReason: null);

        var scopeRepo = new StubScopeStateRepo(scope);
        var discovery = new FakeLegacyChangeDiscoveryPort
        {
            FingerprintToReturn = position,
            DiscoveryResult = new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.Undeterminable,
                CurrentFingerprint: null,
                Deltas: Array.Empty<LegacyDiscoveredDeltaType>(),
                Explanation: "Unit: undeterminable freshness.")
        };
        var legacyRead = new FakeLegacyStockReadPort();
        var idempotency = new StubIdempotencyRepo();
        var syncCalls = 0;

        var gate = new LegacyStockFreshnessGate(
            scopeRepo,
            discovery,
            legacyRead,
            idempotency,
            (_, _) =>
            {
                syncCalls++;
                throw new InvalidOperationException("Sync must not be invoked for Undeterminable discovery.");
            });

        var result = await gate.EnsureFreshAsync(key, decisionContext: "unit-undet");

        result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent);
        result.IsSafeToTrustLedgerLayers.Should().BeFalse();
        result.Explanation.Should().Contain("undeterminable");
        result.Explanation.Should().Contain("unit-undet");
        syncCalls.Should().Be(0);
    }

    [Fact]
    public async Task SyncInconsistent_ReturnsInconsistent()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("INC");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var prior = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;
            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 4m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var syncThrow = CreateSyncHandler(harness.LegacyRead, throwingReconcile: true);
            var discovery = new LegacyChangeDiscoveryPort(
                harness.LegacyRead,
                new StockSourceIdempotencyDal(ConnStringHelper.GetTestEnv()));
            var gate = new LegacyStockFreshnessGate(
                harness.Repos.Scope,
                discovery,
                harness.LegacyRead,
                harness.Repos.Idempotency,
                syncThrow);

            var result = await gate.EnsureFreshAsync(key);

            result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.Inconsistent);
            result.IsSafeToTrustLedgerLayers.Should().BeFalse();
            harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition.Should().Be(prior);
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task PreSyncInconsistentScope_ReturnsInconsistent_WithoutCatchUp()
    {
        var key = StockLedgerScopeKeyType.Create("UT-INCON-BRG", "UT-INCON-RS");
        var position = SynchronizationPositionType.CreateFromUtf8Token(
            "unit-fingerprint",
            LegacyReconstructionBasisCalculator.AlgorithmVersion);
        var scope = StockLedgerScopeStateModel.Rehydrate(
            key.BrgId,
            key.ReceiptSourceId,
            ReconstructionStatusEnum.Reconstructed,
            SynchronizationStateEnum.Inconsistent,
            position,
            reconstructionBasisVersion: LegacyReconstructionBasisCalculator.AlgorithmVersion,
            inconsistencyReason: "Pre-existing material drift.");

        var syncCalls = 0;
        var gate = new LegacyStockFreshnessGate(
            new StubScopeStateRepo(scope),
            new FakeLegacyChangeDiscoveryPort { FingerprintToReturn = position },
            new FakeLegacyStockReadPort(),
            new StubIdempotencyRepo(),
            (_, _) =>
            {
                syncCalls++;
                throw new InvalidOperationException("Sync must not run when Scope is already Inconsistent.");
            });

        var result = await gate.EnsureFreshAsync(key);

        result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.Inconsistent);
        result.IsSafeToTrustLedgerLayers.Should().BeFalse();
        result.Explanation.Should().Contain("Pre-existing material drift");
        syncCalls.Should().Be(0);
    }

    [Fact]
    public async Task Gate_DoesNotMutateLegacyAuthority()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("LEG");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var gate = CreateGate(harness, harness.Sync.Handle);

            await gate.EnsureFreshAsync(key);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 2m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;
            await gate.EnsureFreshAsync(key);

            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    private static LegacyStockFreshnessGate CreateGate(
        Harness harness,
        Func<SynchronizeStockLedgerScopeCommand, CancellationToken, Task<SynchronizeStockLedgerScopeResult>> synchronize)
    {
        var discovery = new LegacyChangeDiscoveryPort(
            harness.LegacyRead,
            new StockSourceIdempotencyDal(ConnStringHelper.GetTestEnv()));
        return new LegacyStockFreshnessGate(
            harness.Repos.Scope,
            discovery,
            harness.LegacyRead,
            harness.Repos.Idempotency,
            synchronize);
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

        var sync = new SynchronizeStockLedgerScopeHandler(
            scopeRepo,
            discovery,
            fakeRead,
            reconcile,
            uow,
            spy,
            snapshotLoader,
            bootstrapper,
            positionRepo,
            idempotencyRepo);

        return new Harness(
            reconstruct,
            sync,
            fakeRead,
            legacyWriter,
            new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo));
    }

    private static SynchronizeStockLedgerScopeHandler CreateSyncHandler(
        FakeLegacyStockReadPort fakeRead,
        bool throwingReconcile)
        => CreateHarness(fakeRead, throwingReconcile).Sync;

    private static Snapshot SingleLocationSnapshot(IStockLedgerScopeKey key)
        => new(
            [Balance(key, "LY01", 10m, 1000m, ExpA, "STK001", T1, "B1")],
            [Journal(key, "TRS001", "LY01", 10m, 0m, 1000m, ExpA, T1, "B1")]);

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
                "Forced material inconsistency for Freshness Gate Inconsistent path.");
    }

    private sealed class StubScopeStateRepo : IStockLedgerScopeStateRepo
    {
        private readonly StockLedgerScopeStateModel _scope;

        public StubScopeStateRepo(StockLedgerScopeStateModel scope) => _scope = scope;

        public void SaveChanges(StockLedgerScopeStateModel model) { }

        public MayBe<StockLedgerScopeStateModel> LoadEntity(IStockLedgerScopeKey id)
            => MayBe<StockLedgerScopeStateModel>.Some(_scope);

        public bool TryInsertNew(StockLedgerScopeStateModel model) => false;

        public bool TryUpdateWhenReconstructionStatus(
            StockLedgerScopeStateModel model,
            ReconstructionStatusEnum expectedPriorStatus)
            => false;

        public bool TryUpdateWhenSynchronizationState(
            StockLedgerScopeStateModel model,
            SynchronizationStateEnum expectedPriorState)
            => false;
    }

    private sealed class StubIdempotencyRepo : IStockSourceIdempotencyRepo
    {
        public MayBe<StockSourceIdempotencyModel> LoadEntity(IStockSourceIdempotencyKey id)
            => MayBe<StockSourceIdempotencyModel>.None;

        public MayBe<StockSourceIdempotencyModel> LoadByBusinessKey(IStockSourceIdempotencyBusinessKey key)
            => MayBe<StockSourceIdempotencyModel>.None;

        public StockSourceIdempotencyInsertResult InsertOrGetExisting(StockSourceIdempotencyModel model)
            => new(false, model);

        public IReadOnlyList<StockSourceIdempotencyModel> ListSyncIdentityRecordsForScope(
            IStockLedgerScopeKey scope)
            => Array.Empty<StockSourceIdempotencyModel>();
    }
}
