using System.Collections.Concurrent;
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
/// P3-S6 — Sync-specific bounded retry, crash resume, concurrent catch-up serialization.
/// Disposable <c>devTest</c> only; never writes <c>tb_stok</c> / <c>tb_buku</c> via sync.
/// Does not activate P3-S7 coexistence harness placeholders.
/// </summary>
[Collection("StockLedgerP3S4")]
public class LegacyStockLedgerSynchronizationRetryTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T3 = new(2024, 2, 1, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);

    [Fact]
    public async Task CrashResume_StuckSynchronizationRequired_CompletesIdempotently()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("CRS");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);

            // Simulate crash after claim, before finalize: Scope stuck SynchronizationRequired,
            // identity coverage incomplete (no SyncBatch keys yet).
            ForceSynchronizationRequired(key);
            CountSyncIdentityKeys(key).Should().Be(0);
            var prior = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;

            var discoveryCalls = 0;
            var countingDiscovery = new CountingDiscoveryPort(harness.Discovery, () => discoveryCalls++);
            var sync = CreateSyncHandler(harness, countingDiscovery);

            var result = await sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            CountSyncIdentityKeys(key).Should().BeGreaterThan(0);
            // Attempt 1 ClaimConflict (no resume) + attempt 2 resume success ⇒ at least 2 rediscovers.
            discoveryCalls.Should().BeGreaterThanOrEqualTo(2);
            // Position may advance after successful finalize (bootstrap path); must not be erased.
            result.SynchronizationPosition.Should().NotBeNull();
            harness.Repos.Scope.LoadEntity(key).Value.SynchronizationState
                .Should().Be(SynchronizationStateEnum.Current);
            // Prior token retained until finalize; after success equals fingerprint-v1 of snapshot.
            var expected = LegacyReconstructionBasisCalculator.Compute(baseline.Balances, baseline.Journals);
            result.SynchronizationPosition.Should().Be(expected);
            prior.Should().Be(expected); // unchanged authority snapshot in this fixture
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task ConcurrentCatchUp_FromCurrent_OneWinner_QuantityNeutral()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("PAR");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            var boot = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            boot.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 5m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var expectedQty = changed.Balances.Sum(b => b.Quantity);
            var bag = new ConcurrentBag<SynchronizeStockLedgerScopeOutcomeEnum>();
            var errors = new ConcurrentBag<Exception>();

            await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                try
                {
                    var racer = CreateFreshSync(harness.LegacyRead);
                    var outcome = await racer.Handle(
                        new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId),
                        default);
                    bag.Add(outcome.Outcome);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));

            errors.Should().BeEmpty();
            bag.Should().HaveCount(8);
            bag.Count(o => o == SynchronizeStockLedgerScopeOutcomeEnum.Synchronized)
                .Should().BeGreaterThanOrEqualTo(1);
            bag.Should().OnlyContain(o =>
                o == SynchronizeStockLedgerScopeOutcomeEnum.Synchronized
                || o == SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent
                || o == SynchronizeStockLedgerScopeOutcomeEnum.ClaimConflict);

            var durable = harness.Repos.Scope.LoadEntity(key).Value;
            durable.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            TotalRemaining(key).Should().Be(expectedQty);

            var expectedPosition = LegacyReconstructionBasisCalculator.Compute(
                changed.Balances, changed.Journals);
            durable.SynchronizationPosition.Should().Be(expectedPosition);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task DuplicateConcurrentCatchUp_DoesNotDoubleApplyQuantity()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("DUP");
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
            var expectedQty = changed.Balances.Sum(b => b.Quantity);

            await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
            {
                var racer = CreateFreshSync(harness.LegacyRead);
                await racer.Handle(
                    new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId),
                    default);
            }));

            TotalRemaining(key).Should().Be(expectedQty);
            harness.Repos.Scope.LoadEntity(key).Value.SynchronizationState
                .Should().Be(SynchronizationStateEnum.Current);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task FreshnessGate_ExhaustedClaimConflict_ReturnsStaleOrNotCurrent()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("GEX");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 2m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var syncCalls = 0;
            var gate = CreateGate(harness, (cmd, ct) =>
            {
                syncCalls++;
                var scope = harness.Repos.Scope.LoadEntity(key).Value;
                return Task.FromResult(SynchronizeStockLedgerScopeResult.ClaimConflict(scope));
            });

            var result = await gate.EnsureFreshAsync(key);

            result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent);
            result.IsSafeToTrustLedgerLayers.Should().BeFalse();
            syncCalls.Should().Be(1, "Gate must invoke catch-up at most once; retries are inside sync.");
            result.Explanation.Should().ContainEquivalentOf("retr");
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task FreshnessGate_InternalRetryClearsTransientConflict_ReturnsSynchronizedNow()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("GRT");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 4m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            // Force stuck SynchronizationRequired so first attempt ClaimConflicts, retry resumes.
            ForceSynchronizationRequired(key);

            var syncCalls = 0;
            var gate = CreateGate(harness, async (cmd, ct) =>
            {
                syncCalls++;
                return await harness.Sync.Handle(cmd, ct);
            });

            var result = await gate.EnsureFreshAsync(key);

            result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.SynchronizedNow);
            result.IsSafeToTrustLedgerLayers.Should().BeTrue();
            syncCalls.Should().Be(1);
            TotalRemaining(key).Should().Be(changed.Balances.Sum(b => b.Quantity));
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task ConcurrentCatchUp_DepletedIntentionalDifference_RemainsBalanced()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("DEP");
        Cleanup(key);

        try
        {
            // Baseline: journal IN 10 / OUT 10 ⇒ depleted layer retained; legacy balance absent.
            var journals = new[]
            {
                Journal(key, "TRS001", "LY01", 10m, 0m, 1000m, ExpA, T1, "B1"),
                Journal(key, "TRS002", "LY01", 0m, 10m, 1000m, ExpA, T3, "B1")
            };
            var balances = Array.Empty<LegacyStockBalanceType>();
            var harness = CreateHarness(new Snapshot(balances, journals));
            await ReconstructAsync(harness, key);

            await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
            {
                var racer = CreateFreshSync(harness.LegacyRead);
                await racer.Handle(
                    new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId),
                    default);
            }));

            var scope = harness.Repos.Scope.LoadEntity(key).Value;
            scope.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            // Depleted layer retained (RemainingQuantity 0); not erased.
            CountLayers(key).Should().BeGreaterThanOrEqualTo(1);
            TotalRemaining(key).Should().Be(0m);
        }
        finally
        {
            Cleanup(key);
        }
    }

    private static void ForceSynchronizationRequired(IStockLedgerScopeKey key)
    {
        var options = ConnStringHelper.GetTestEnv();
        var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
        var current = scopeRepo.LoadEntity(key).Value;
        current.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        var next = current.RequireSynchronization();
        scopeRepo.TryUpdateWhenSynchronizationState(next, SynchronizationStateEnum.Current)
            .Should().BeTrue();
    }

    private static async Task ReconstructAsync(Harness harness, IStockLedgerScopeKey key)
    {
        var result = await harness.Reconstruct.Handle(
            new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
        result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);
    }

    private static LegacyStockFreshnessGate CreateGate(
        Harness harness,
        Func<SynchronizeStockLedgerScopeCommand, CancellationToken, Task<SynchronizeStockLedgerScopeResult>> sync)
        => new(
            harness.Repos.Scope,
            harness.Discovery,
            harness.LegacyRead,
            harness.Repos.Idempotency,
            sync);

    private static Harness CreateHarness(Snapshot snapshot)
    {
        var fakeRead = new FakeLegacyStockReadPort
        {
            Balances = snapshot.Balances,
            JournalEntries = snapshot.Journals
        };
        return CreateHarness(fakeRead);
    }

    private static Harness CreateHarness(FakeLegacyStockReadPort fakeRead)
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
        var reconcile = new StockReconciliationPort(fakeRead, new StockLayerDal(options), scopeRepo);
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
            discovery,
            fakeRead,
            new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo));
    }

    private static SynchronizeStockLedgerScopeHandler CreateSyncHandler(
        Harness harness,
        ILegacyChangeDiscoveryPort discovery)
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
        var reconcile = new StockReconciliationPort(
            harness.LegacyRead, new StockLayerDal(options), scopeRepo);
        var snapshotLoader = new LegacySyncLedgerSnapshotLoader(
            idempotencyRepo, positionRepo, movementRepo);
        var bootstrapper = new LegacySyncIdentityBootstrapper(
            uow, idempotencyRepo, movementRepo);
        var syncClaim = new SynchronizationClaimService(spy, scopeRepo);

        return new SynchronizeStockLedgerScopeHandler(
            scopeRepo,
            discovery,
            harness.LegacyRead,
            reconcile,
            uow,
            spy,
            syncClaim,
            snapshotLoader,
            bootstrapper,
            positionRepo,
            idempotencyRepo);
    }

    private static SynchronizeStockLedgerScopeHandler CreateFreshSync(FakeLegacyStockReadPort fakeRead)
        => CreateHarness(fakeRead).Sync;

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
            .Concat([Journal(key, "TRS-NEW", "LY01", qtyIn, 0m, 1000m, ExpA, T3, "B1")])
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
        ILegacyChangeDiscoveryPort Discovery,
        FakeLegacyStockReadPort LegacyRead,
        Repos Repos);

    private sealed class CountingDiscoveryPort : ILegacyChangeDiscoveryPort
    {
        private readonly ILegacyChangeDiscoveryPort _inner;
        private readonly Action _onDiscover;

        public CountingDiscoveryPort(ILegacyChangeDiscoveryPort inner, Action onDiscover)
        {
            _inner = inner;
            _onDiscover = onDiscover;
        }

        public SynchronizationPositionType ComputeCurrentFingerprint(IStockLedgerScopeKey scope)
            => _inner.ComputeCurrentFingerprint(scope);

        public LegacyChangeDiscoveryResult DiscoverChanges(
            IStockLedgerScopeKey scope,
            SynchronizationPositionType? storedPosition)
        {
            _onDiscover();
            return _inner.DiscoverChanges(scope, storedPosition);
        }
    }
}
