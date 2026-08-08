using System.Collections.Concurrent;
using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.InventoryContext.StockLedgerFeature.Fakes;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// G-23 coexistence harness — Phase 3 sync portion (P3-S7) + Phase 4 New→Legacy / partial-failure (P4-S3).
/// <para>
/// Active: Legacy→New (discovery + catch-up + Freshness Gate), duplicate sync batch,
/// real material mismatch, .NET sync/native-serialization race (in-process doubles),
/// P2-S8 depleted-layer intentional difference, New→Legacy receipt visibility,
/// and live legacy+Ledger partial-failure rollback.
/// </para>
/// <para>
/// Remaining skipped placeholders (later-phase ownership):
/// <list type="bullet">
/// <item><description><c>AlternatingWriters</c> — Phase 4+ / P4-S5 (native FO + fixture-simulated VB6)</description></item>
/// <item><description><c>ConcurrentOutbound</c> — Phase 5 (outbound allocation + OCC hardening)</description></item>
/// </list>
/// Does not claim full G-23 matrix or production coexistence (FQ-06 / G-17 remains Phase 9).
/// </para>
/// </summary>
[Collection("StockLedgerP3S4")]
public class StockLedgerCoexistenceHarnessPlaceholderTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T3 = new(2024, 2, 1, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);
    private static readonly DateOnly ExpB = new(2025, 6, 1);

    /// <summary>
    /// G-23 Legacy→New — VB6-side change is consumed by discovery, catch-up, and Freshness Gate;
    /// Ledger reflects <see cref="StockFactOriginEnum.LegacySynchronized"/> facts; legacy authority is never blocked.
    /// </summary>
    [Fact]
    public async Task LegacyToNew_SynchronizesLegacyOriginatedChange()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("L2N");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateSyncHarness(baseline);
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

            var result = await gate.EnsureFreshAsync(key, "LegacyToNewHarness");

            result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.SynchronizedNow);
            result.IsSafeToTrustLedgerLayers.Should().BeTrue();
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            result.SynchronizationPosition.Should().NotBe(priorPosition);

            var expectedPosition = LegacyReconstructionBasisCalculator.Compute(
                changed.Balances, changed.Journals);
            result.SynchronizationPosition.Should().Be(expectedPosition);
            result.SynchronizationPosition!.AlgorithmVersion
                .Should().Be(LegacyReconstructionBasisCalculator.AlgorithmVersion);

            syncCalls.Should().Be(1, "Freshness Gate must invoke catch-up at most once per call.");
            TotalRemaining(key).Should().Be(changed.Balances.Sum(b => b.Quantity));

            var syncOrigins = ListSyncMovementOrigins(key);
            syncOrigins.Should().Contain(StockFactOriginEnum.LegacySynchronized);
            harness.LegacyWriter.Applied.Should().BeEmpty("sync must not block or rewrite legacy authority.");
        }
        finally
        {
            Cleanup(key);
        }
    }

    /// <summary>
    /// G-23 New→Legacy (P4-S3) — Native DO Receipt posts authoritative legacy rows visible to
    /// <see cref="LegacyStockReadPort"/>; Ledger movement Origin remains Native (not authority).
    /// </summary>
    [Fact]
    public async Task NewToLegacy_ReceiptVisibleInLegacyAuthority()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var caseIds = CreateDoReceiptCaseIds("N2L");
        CleanupDoReceipt(caseIds);

        try
        {
            var harness = CreateLiveDoReceiptHarness(enabled: true);
            var result = await harness.Handler.Handle(BuildDoReceiptCommand(caseIds), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);
            result.StockMovementId.Should().NotBeNullOrWhiteSpace();

            var balances = harness.LegacyRead.ListCurrentBalances(caseIds.Scope);
            var journals = harness.LegacyRead.ListJournalEntries(caseIds.Scope);

            balances.Should().ContainSingle();
            balances[0].Quantity.Should().Be(10m);
            balances[0].UnitCost.Should().Be(1500.50m);
            balances[0].ReceiptSourceId.Should().Be(caseIds.DoId);
            balances[0].LayananId.Should().Be(caseIds.LocationId);

            journals.Should().ContainSingle();
            journals[0].MutationKindId.Should().Be("DO");
            journals[0].QuantityIn.Should().Be(10m);
            journals[0].QuantityOut.Should().Be(0m);

            var movement = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(result.StockMovementId!));
            movement.HasValue.Should().BeTrue();
            movement.Value.Origin.Should().Be(StockFactOriginEnum.Native);
        }
        finally
        {
            CleanupDoReceipt(caseIds);
        }
    }

    [Fact(Skip = "G-23 — Phase 4+: alternating writers requires native FO writer + live mixed-writer enablement.")]
    public void AlternatingWriters_RemainReconcileable()
    {
        Assert.Fail("Not implemented: alternating writers scenario (Phase 4+).");
    }

    /// <summary>
    /// P2-S8 / G-16 reconstruction portion — intentional representational difference:
    /// legacy omits depleted zero <c>tb_stok</c> rows; Ledger retains Remaining = 0 layers.
    /// </summary>
    [Fact]
    public async Task DepletedLayer_IntentionalLegacyDifference_IsReconciledAsRepresentational()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("DEP");
        Cleanup(key);

        try
        {
            var legacyBalances = new List<LegacyStockBalanceType>
            {
                Balance(key, "LY01", 5m, 1000m, ExpA, "STK001", T1, "B1")
            };
            var journals = new List<LegacyStockJournalEntryType>
            {
                Journal(key, "TRS001", "LY01", qtyIn: 5m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
                Journal(key, "TRS002", "LY02", qtyIn: 8m, qtyOut: 0m, 1200m, ExpB, T2, "B2"),
                Journal(key, "TRS003", "LY02", qtyIn: 0m, qtyOut: 8m, 1200m, ExpB, T3, "B2")
            };

            legacyBalances.Should().NotContain(b => b.LayananId == "LY02",
                "intentional difference: depleted zero row absent from legacy balances");

            var (sut, _, _, repos) = CreateReconstructionSut(legacyBalances, journals);
            var result = await sut.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);

            var ly01 = repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY01"))
                .Value;
            ly01.TotalRemainingQuantity.Should().Be(5m);
            ly01.Layers.Should().ContainSingle(l => !l.IsDepleted);

            var ly02 = repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY02"))
                .Value;
            ly02.Layers.Should().ContainSingle();
            ly02.Layers[0].IsDepleted.Should().BeTrue();
            ly02.Layers[0].RemainingQuantity.Should().Be(0m);
            ly02.Layers[0].InitialQuantity.Should().Be(8m);
            ly02.Layers[0].Origin.Should().Be(StockFactOriginEnum.Reconstructed);

            legacyBalances.Should().HaveCount(1);
            CountLayers(key).Should().Be(2);
        }
        finally
        {
            Cleanup(key);
        }
    }

    /// <summary>
    /// G-23 real mismatch — material quantity drift surfaces Inconsistent via catch-up + Freshness Gate;
    /// prior Synchronization Position is retained.
    /// </summary>
    [Fact]
    public async Task RealMismatch_IsClassifiedAndSurfaced()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("MIS");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateSyncHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var priorPosition = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition!;

            // Simulate undetected Ledger drift (not mirrored in legacy authority).
            TamperLayerRemainingQuantity(key, "LY01", newRemaining: 1m);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 5m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var gate = CreateGate(harness, harness.Sync.Handle);
            var result = await gate.EnsureFreshAsync(key, "RealMismatchHarness");

            result.Outcome.Should().Be(LegacyStockFreshnessGateOutcomeEnum.Inconsistent);
            result.IsSafeToTrustLedgerLayers.Should().BeFalse();

            var scope = harness.Repos.Scope.LoadEntity(key).Value;
            scope.SynchronizationState.Should().Be(SynchronizationStateEnum.Inconsistent);
            scope.SynchronizationPosition.Should().Be(priorPosition);

            var reconcile = new StockReconciliationPort(
                harness.LegacyRead,
                new StockLayerDal(ConnStringHelper.GetTestEnv()),
                harness.Repos.Scope);
            var classification = reconcile.Reconcile(key);
            classification.Outcome.Should().Be(StockReconciliationOutcomeEnum.MaterialInconsistency);
            StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(classification)
                .Should().BeFalse();
        }
        finally
        {
            Cleanup(key);
        }
    }

    /// <summary>
    /// G-23 duplicate sync batch — repeated catch-up against the same legacy snapshot is quantity-neutral.
    /// </summary>
    [Fact]
    public async Task DuplicateSyncBatch_IsIdempotent()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("DUP");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateSyncHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 3m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;

            var first = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            first.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.Synchronized);

            var layersAfterFirst = CountLayers(key);
            var qtyAfterFirst = TotalRemaining(key);
            var positionAfterFirst = harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition;

            var second = await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
            second.Outcome.Should().Be(SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent);

            CountLayers(key).Should().Be(layersAfterFirst);
            TotalRemaining(key).Should().Be(qtyAfterFirst);
            harness.Repos.Scope.LoadEntity(key).Value.SynchronizationPosition.Should().Be(positionAfterFirst);
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact(Skip = "G-23 — Phase 5: concurrent outbound requires Phase 5 allocation + OCC hardening.")]
    public void ConcurrentOutbound_RespectsWriteConsistencyScope()
    {
        Assert.Fail("Not implemented: concurrent outbound scenario (Phase 5).");
    }

    /// <summary>
    /// G-23 PartialFailure (P4-S3) — live legacy Apply failure inside the consequence UoW rolls back
    /// both Ledger and <c>tb_stok</c>/<c>tb_buku</c>. Full injection matrix lives in
    /// <see cref="StockConsequenceUnitOfWorkLiveAtomicityTest"/>.
    /// </summary>
    [Fact]
    public void PartialFailure_LegacyAndLedger_RollBackTogether()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = StockConsequenceUnitOfWorkLiveAtomicityTest.CreateCaseIds("PF");
        StockConsequenceUnitOfWorkLiveAtomicityTest.Cleanup(ids);

        try
        {
            var options = ConnStringHelper.GetTestEnv();
            var movementRepo = new StockMovementRepo(
                new StockMovementDal(options),
                new StockMovementLineDal(options));
            var positionRepo = new StockPositionRepo(
                new StockPositionDal(options),
                new StockLayerDal(options));
            var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
            var idempotencyRepo = new StockSourceIdempotencyRepo(new StockSourceIdempotencyDal(options));
            var liveWriter = new LegacyCompatibilityWriterPort(options);
            var throwingWriter = new ThrowingLegacyCompatibilityWriterPort(liveWriter);
            var sut = new StockConsequenceUnitOfWork(
                new TransHelperUnitOfWork(),
                idempotencyRepo,
                movementRepo,
                positionRepo,
                scopeRepo,
                throwingWriter);

            var draft = StockConsequenceUnitOfWorkLiveAtomicityTest.BuildReceiptDraft(ids, lineCount: 1);
            var act = () => sut.Commit(draft);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Forced legacy Apply failure*");

            StockConsequenceUnitOfWorkLiveAtomicityTest.AssertZeroResidue(ids);
        }
        finally
        {
            StockConsequenceUnitOfWorkLiveAtomicityTest.Cleanup(ids);
        }
    }

    /// <summary>
    /// G-23 / G-17 interim (.NET-side only) — in-process native-write doubles honor Freshness Gate
    /// serialization while concurrent catch-up runs; quantity remains correct; FQ-06 not claimed.
    /// </summary>
    [Fact]
    public async Task SyncNativeSerializationRace_InProcessDoubles_OneWinnerQuantityNeutral()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("RCE");
        Cleanup(key);

        try
        {
            var baseline = SingleLocationSnapshot(key);
            var harness = CreateSyncHarness(baseline);
            await ReconstructAsync(harness, key);
            await harness.Sync.Handle(
                new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);

            var changed = ChangedWithJournalInsert(key, baseline, qtyIn: 4m);
            harness.LegacyRead.Balances = changed.Balances;
            harness.LegacyRead.JournalEntries = changed.Journals;
            var expectedQty = changed.Balances.Sum(b => b.Quantity);

            var gateOutcomes = new ConcurrentBag<LegacyStockFreshnessGateOutcomeEnum>();
            var syncOutcomes = new ConcurrentBag<SynchronizeStockLedgerScopeOutcomeEnum>();
            var errors = new ConcurrentBag<Exception>();

            await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
            {
                try
                {
                    if (i % 2 == 0)
                    {
                        var racerHarness = CreateSyncHarness(harness.LegacyRead);
                        var gate = CreateGate(racerHarness, racerHarness.Sync.Handle);
                        var gateResult = await gate.EnsureFreshAsync(key, "NativeWriteDouble");
                        gateOutcomes.Add(gateResult.Outcome);
                    }
                    else
                    {
                        var racer = CreateSyncHarness(harness.LegacyRead).Sync;
                        var syncResult = await racer.Handle(
                            new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId),
                            default);
                        syncOutcomes.Add(syncResult.Outcome);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));

            errors.Should().BeEmpty();
            gateOutcomes.Should().NotBeEmpty();
            syncOutcomes.Should().NotBeEmpty();

            gateOutcomes.Should().OnlyContain(o =>
                o == LegacyStockFreshnessGateOutcomeEnum.SynchronizedNow
                || o == LegacyStockFreshnessGateOutcomeEnum.Current
                || o == LegacyStockFreshnessGateOutcomeEnum.StaleOrNotCurrent);

            syncOutcomes.Should().OnlyContain(o =>
                o == SynchronizeStockLedgerScopeOutcomeEnum.Synchronized
                || o == SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent
                || o == SynchronizeStockLedgerScopeOutcomeEnum.ClaimConflict);

            (gateOutcomes.Count(o => o == LegacyStockFreshnessGateOutcomeEnum.SynchronizedNow)
             + syncOutcomes.Count(o => o == SynchronizeStockLedgerScopeOutcomeEnum.Synchronized))
                .Should().BeGreaterThanOrEqualTo(1);

            var scope = harness.Repos.Scope.LoadEntity(key).Value;
            scope.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            TotalRemaining(key).Should().Be(expectedQty);

            var expectedPosition = LegacyReconstructionBasisCalculator.Compute(
                changed.Balances, changed.Journals);
            scope.SynchronizationPosition.Should().Be(expectedPosition);
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    private static async Task ReconstructAsync(SyncHarness harness, IStockLedgerScopeKey key)
    {
        var result = await harness.Reconstruct.Handle(
            new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
        result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);
    }

    private static LegacyStockFreshnessGate CreateGate(
        SyncHarness harness,
        Func<SynchronizeStockLedgerScopeCommand, CancellationToken, Task<SynchronizeStockLedgerScopeResult>> sync)
        => new(
            harness.Repos.Scope,
            harness.Discovery,
            harness.LegacyRead,
            harness.Repos.Idempotency,
            sync);

    private static SyncHarness CreateSyncHarness(Snapshot snapshot)
        => CreateSyncHarness(new FakeLegacyStockReadPort
        {
            Balances = snapshot.Balances,
            JournalEntries = snapshot.Journals
        });

    private static SyncHarness CreateSyncHarness(FakeLegacyStockReadPort fakeRead)
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

        return new SyncHarness(
            reconstruct,
            sync,
            discovery,
            fakeRead,
            legacyWriter,
            new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo));
    }

    private static (
        ReconstructStockLedgerBaselineHandler Sut,
        SpyUnitOfWork Spy,
        FakeLegacyCompatibilityWriterPort Legacy,
        Repos Repos)
        CreateReconstructionSut(
            IReadOnlyList<LegacyStockBalanceType> balances,
            IReadOnlyList<LegacyStockJournalEntryType> journals)
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
        var idempotencyRepo = new StockSourceIdempotencyRepo(new StockSourceIdempotencyDal(options));
        var legacyWriter = new FakeLegacyCompatibilityWriterPort();
        var fakeRead = new FakeLegacyStockReadPort
        {
            Balances = balances,
            JournalEntries = journals
        };

        var claim = new ReconstructionClaimService(spy, scopeRepo);
        var uow = new StockConsequenceUnitOfWork(
            spy,
            idempotencyRepo,
            movementRepo,
            positionRepo,
            scopeRepo,
            legacyWriter);
        var sut = new ReconstructStockLedgerBaselineHandler(
            claim,
            fakeRead,
            uow,
            spy,
            scopeRepo,
            idempotencyRepo);

        return (sut, spy, legacyWriter, new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo));
    }

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

    private static void TamperLayerRemainingQuantity(
        IStockLedgerScopeKey key,
        string layananId,
        decimal newRemaining)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        conn.Execute(
            """
            UPDATE BILRG_StokLayer
            SET RemainingQuantity = @RemainingQuantity
            WHERE BrgId = @BrgId
              AND ReceiptSourceId = @ReceiptSourceId
              AND LayananId = @LayananId
            """,
            new
            {
                RemainingQuantity = newRemaining,
                key.BrgId,
                key.ReceiptSourceId,
                LayananId = layananId
            }).Should().BeGreaterThan(0);
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
        var shortTag = tag.Length <= 3 ? tag : tag[..3];
        return StockLedgerScopeKeyType.Create(
            $"BRGG23{shortTag}{ulid}"[..13],
            $"DOG23{shortTag}{ulid}"[..10]);
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
            """
            SELECT COUNT(1) FROM BILRG_StokLayer
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
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

    private static IReadOnlyList<StockFactOriginEnum> ListSyncMovementOrigins(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.Query<int>(
            """
            SELECT DISTINCT m.Origin
            FROM BILRG_StokMovement m
            INNER JOIN BILRG_StokSourceIdempotency i
              ON i.StockMovementId = m.StockMovementId
            WHERE i.BrgId = @BrgId
              AND i.ReceiptSourceId = @ReceiptSourceId
              AND i.IdempotencyKind = @SyncKind
            """,
            new
            {
                key.BrgId,
                key.ReceiptSourceId,
                SyncKind = (int)StockSourceIdempotencyKindEnum.SyncBatch
            })
            .Select(o => (StockFactOriginEnum)o)
            .ToList();
    }

    private static readonly DateTime DoReceiptBusinessTime = new(2026, 8, 8, 15, 0, 0);
    private static readonly DateTime DoReceiptProcessedAt = new(2026, 8, 8, 15, 5, 0);

    private static PostDoReceiptStockConsequenceCommand BuildDoReceiptCommand(DoReceiptCaseIds ids)
        => new(
            ids.BrgId,
            ids.DoId,
            ids.SourceTxId,
            DoReceiptBusinessTime,
            [
                new DoReceiptLineFact(
                    LineNumber: 1,
                    LayananId: ids.LocationId,
                    Quantity: 10m,
                    UnitCost: 1500.50m,
                    ExpirationDate: new DateOnly(2027, 6, 30),
                    Batch: "P4S3-N2L",
                    PurchaseOrderId: ids.PoId,
                    SmallestUnitId: "TAB")
            ],
            DoReceiptProcessedAt);

    private static LiveDoReceiptHarness CreateLiveDoReceiptHarness(bool enabled)
    {
        var dbOptions = ConnStringHelper.GetTestEnv();
        var movementRepo = new StockMovementRepo(
            new StockMovementDal(dbOptions),
            new StockMovementLineDal(dbOptions));
        var positionRepo = new StockPositionRepo(
            new StockPositionDal(dbOptions),
            new StockLayerDal(dbOptions));
        var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(dbOptions));
        var idempotencyRepo = new StockSourceIdempotencyRepo(new StockSourceIdempotencyDal(dbOptions));
        var unitOfWork = new TransHelperUnitOfWork();
        var legacyWriter = new LegacyCompatibilityWriterPort(dbOptions);
        var legacyRead = new LegacyStockReadPort(dbOptions);

        var consequenceUow = new StockConsequenceUnitOfWork(
            unitOfWork,
            idempotencyRepo,
            movementRepo,
            positionRepo,
            scopeRepo,
            legacyWriter);

        var bootstrapper = new LegacySyncIdentityBootstrapper(
            consequenceUow,
            idempotencyRepo,
            movementRepo);

        var discovery = new FakeLegacyChangeDiscoveryPort
        {
            DiscoveryResult = new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.Unchanged,
                CurrentFingerprint: SynchronizationPositionType.CreateFromUtf8Token(
                    "unused",
                    LegacyReconstructionBasisCalculator.AlgorithmVersion),
                Deltas: Array.Empty<LegacyDiscoveredDeltaType>(),
                Explanation: "Fake default Unchanged.")
        };

        var gate = new LegacyStockFreshnessGate(
            scopeRepo,
            discovery,
            legacyRead,
            idempotencyRepo,
            (_, _) => Task.FromResult(
                SynchronizeStockLedgerScopeResult.AlreadyCurrent(
                    StockLedgerScopeStateModel.CreateNotReconstructed(
                        StockLedgerScopeKeyType.Create("X", "Y")))));

        var handler = new PostDoReceiptStockConsequenceHandler(
            Options.Create(new StockLedgerDoReceiptOptions { Enabled = enabled }),
            scopeRepo,
            idempotencyRepo,
            positionRepo,
            consequenceUow,
            gate,
            legacyRead,
            bootstrapper);

        return new LiveDoReceiptHarness(
            handler,
            legacyRead,
            new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo));
    }

    private static DoReceiptCaseIds CreateDoReceiptCaseIds(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 3 ? tag : tag[..3];
        var brgId = $"BRG{shortTag}{ulid}"[..13];
        var doId = $"DO{shortTag}{ulid}"[..10];
        return new DoReceiptCaseIds(
            SourceTxId: $"P4S3-{tag}-{ulid}",
            BrgId: brgId,
            DoId: doId,
            PoId: $"PO{shortTag}{ulid}"[..10],
            LocationId: $"G{shortTag}{ulid}"[..5],
            Scope: StockLedgerScopeKeyType.Create(brgId, doId));
    }

    private static void CleanupDoReceipt(DoReceiptCaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        conn.Execute(
            """
            DELETE FROM BILRG_StokMovementLine
            WHERE StockMovementId IN (
                SELECT StockMovementId FROM BILRG_StokSourceIdempotency
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId AND StockMovementId <> ''
                UNION
                SELECT LayerFormingMovementId FROM BILRG_StokLayer
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
                UNION
                SELECT StockMovementId FROM BILRG_StokMovement
                WHERE SourceTransactionId = @SourceTxId);
            DELETE FROM BILRG_StokMovement
            WHERE StockMovementId IN (
                SELECT StockMovementId FROM BILRG_StokSourceIdempotency
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId AND StockMovementId <> ''
                UNION
                SELECT LayerFormingMovementId FROM BILRG_StokLayer
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
                UNION
                SELECT StockMovementId FROM BILRG_StokMovement
                WHERE SourceTransactionId = @SourceTxId);
            DELETE FROM BILRG_StokLayer WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokPosition WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokLedgerScope WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokSourceIdempotency
            WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM tb_stok WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId;
            DELETE FROM tb_buku WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId;
            """,
            new
            {
                ids.BrgId,
                ids.DoId,
                ids.SourceTxId
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

    private sealed record SyncHarness(
        ReconstructStockLedgerBaselineHandler Reconstruct,
        SynchronizeStockLedgerScopeHandler Sync,
        ILegacyChangeDiscoveryPort Discovery,
        FakeLegacyStockReadPort LegacyRead,
        FakeLegacyCompatibilityWriterPort LegacyWriter,
        Repos Repos);

    private sealed record DoReceiptCaseIds(
        string SourceTxId,
        string BrgId,
        string DoId,
        string PoId,
        string LocationId,
        IStockLedgerScopeKey Scope);

    private sealed record LiveDoReceiptHarness(
        PostDoReceiptStockConsequenceHandler Handler,
        LegacyStockReadPort LegacyRead,
        Repos Repos);
}
