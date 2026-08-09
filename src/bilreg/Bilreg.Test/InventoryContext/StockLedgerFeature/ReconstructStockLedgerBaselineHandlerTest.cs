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
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S6 — Phase B/C orchestration + persist (G-10 core).
/// Disposable <c>devTest</c> only; never writes <c>tb_stok</c> / <c>tb_buku</c> via reconstruction.
/// </summary>
public class ReconstructStockLedgerBaselineHandlerTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T3 = new(2024, 2, 1, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);
    private static readonly DateOnly ExpB = new(2025, 6, 1);

    [Fact]
    public async Task HappyPath_PersistsReconstructedBaseline_AndInitializesSyncPosition()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var key = NewScopeKey("HAP");
        Cleanup(key);

        try
        {
            var snapshot = MultiLocationBalancedSnapshot(key);
            var (sut, spy, legacy, repos) = CreateSut(snapshot);

            var result = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);
            result.ScopeState.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
            result.SynchronizationPosition.Should().NotBeNull();
            result.SynchronizationPosition!.AlgorithmVersion
                .Should().Be(LegacyReconstructionBasisCalculator.AlgorithmVersion);
            result.ScopeState.ReconstructionBasisVersion
                .Should().Be(LegacyReconstructionBasisCalculator.AlgorithmVersion);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);

            var loaded = repos.Scope.LoadEntity(key).Value;
            loaded.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
            loaded.HasSynchronizationPosition.Should().BeTrue();
            loaded.SynchronizationPosition.Should().Be(result.SynchronizationPosition);

            var expectedBasis = LegacyReconstructionBasisCalculator.Compute(snapshot.Balances, snapshot.Journals);
            loaded.SynchronizationPosition.Should().Be(expectedBasis);

            repos.Movement.LoadEntity(StockMovementModel.Key(
                    LegacyReconstructionBaselineCalculator.BuildMovementId(key)))
                .HasValue.Should().BeTrue();
            repos.Position.LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY01"))
                .HasValue.Should().BeTrue();
            repos.Position.LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY02"))
                .HasValue.Should().BeTrue();

            repos.Idempotency.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(
                        StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                        BuildIdempotencyKey(key)))
                .HasValue.Should().BeTrue();

            legacy.Applied.Should().BeEmpty();
            spy.BeginCount.Should().BeGreaterThanOrEqualTo(2); // Phase A + Phase C
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task DuplicateReconstruction_IsIdempotent_WithoutDuplicatingQuantityOrLayers()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("IDM");
        Cleanup(key);

        try
        {
            var snapshot = MultiLocationBalancedSnapshot(key);
            var (sut, _, _, repos) = CreateSut(snapshot);

            var first = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
            first.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);

            var layersAfterFirst = CountLayers(key);
            var positionsAfterFirst = CountPositions(key);

            var second = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
            second.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed);
            second.ScopeState.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
            second.SynchronizationPosition.Should().Be(first.SynchronizationPosition);

            CountLayers(key).Should().Be(layersAfterFirst);
            CountPositions(key).Should().Be(positionsAfterFirst);
            CountMovements(key).Should().Be(1);

            repos.Idempotency.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(
                        StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                        BuildIdempotencyKey(key)))
                .HasValue.Should().BeTrue();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task BasisChangeDuringPhaseB_DoesNotPersistStaleBaseline()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("BAS");
        Cleanup(key);

        try
        {
            var phaseB = MultiLocationBalancedSnapshot(key);
            var changed = phaseB with
            {
                Balances =
                [
                    Balance(key, "LY01", 9m, 1000m, ExpA, "STK001", T1, "B1"),
                    Balance(key, "LY02", 7m, 1500m, ExpB, "STK002", T2, "B2")
                ]
            };

            // Balance and journal counters are independent. Per attempt:
            // call 1 = Phase B snapshot, call 2 = Phase C revalidate (changed) → basis mismatch.
            // Retries keep mismatch → BasisChangedRetryRequired; Scope stays Reconstructing.
            var fakeRead = new FakeLegacyStockReadPort
            {
                BalancesByCall = call => call % 2 == 1 ? phaseB.Balances : changed.Balances,
                JournalsByCall = call => call % 2 == 1 ? phaseB.Journals : changed.Journals
            };

            var (sut, _, _, repos) = CreateSut(fakeRead);

            var result = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.BasisChangedRetryRequired);
            repos.Scope.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructing);
            CountLayers(key).Should().Be(0);
            CountPositions(key).Should().Be(0);
            CountMovements(key).Should().Be(0);
            repos.Idempotency.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(
                        StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                        BuildIdempotencyKey(key)))
                .HasValue.Should().BeFalse();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task InconsistentBaseline_MarksScopeInconsistent_WithoutFakeBalancedLayers()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("INC");
        Cleanup(key);

        try
        {
            var snapshot = new Snapshot(
                [
                    Balance(key, "LY01", 10m, 1000m, ExpA, "STK001", T1, "B1")
                ],
                [
                    Journal(key, "TRS001", "LY01", qtyIn: 10m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
                    Journal(key, "TRS002", "LY01", qtyIn: 0m, qtyOut: 3m, 1000m, ExpA, T2, "B1")
                ]);

            var (sut, _, _, repos) = CreateSut(snapshot);

            var result = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Inconsistent);
            result.InconsistencyReason.Should().NotBeNullOrWhiteSpace();
            result.ScopeState.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Inconsistent);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Inconsistent);
            result.ScopeState.HasSynchronizationPosition.Should().BeFalse();

            CountLayers(key).Should().Be(0);
            CountPositions(key).Should().Be(0);
            CountMovements(key).Should().Be(0);

            var second = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
            second.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.AlreadyInconsistent);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task DepletedLayers_ArePersisted_WhenAbsentFromLegacyBalances()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("DEP");
        Cleanup(key);

        try
        {
            var snapshot = new Snapshot(
                [
                    Balance(key, "LY01", 5m, 1000m, ExpA, "STK001", T1, "B1")
                ],
                [
                    Journal(key, "TRS001", "LY01", qtyIn: 5m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
                    Journal(key, "TRS002", "LY02", qtyIn: 8m, qtyOut: 0m, 1200m, ExpB, T2, "B2"),
                    Journal(key, "TRS003", "LY02", qtyIn: 0m, qtyOut: 8m, 1200m, ExpB, T3, "B2")
                ]);

            var (sut, _, _, repos) = CreateSut(snapshot);

            var result = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);

            var ly02 = repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY02"))
                .Value;
            ly02.Layers.Should().ContainSingle();
            ly02.Layers[0].IsDepleted.Should().BeTrue();
            ly02.Layers[0].RemainingQuantity.Should().Be(0m);
            ly02.Layers[0].InitialQuantity.Should().Be(8m);
            ly02.Layers[0].Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task PhaseCPersistFailure_RollsBackAdditiveLedgerChanges()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("ROL");
        Cleanup(key);

        try
        {
            var snapshot = MultiLocationBalancedSnapshot(key);
            var options = ConnStringHelper.GetTestEnv();
            var spy = new SpyUnitOfWork();
            var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
            var movementRepo = new StockMovementRepo(
                new StockMovementDal(options),
                new StockMovementLineDal(options));
            var realPositionRepo = new StockPositionRepo(
                new StockPositionDal(options),
                new StockLayerDal(options));
            var positionRepo = new Fakes.ThrowingStockPositionRepo(
                realPositionRepo,
                "Forced position persist failure for P2-S6 rollback test.");
            var idempotencyRepo = new StockSourceIdempotencyRepo(new StockSourceIdempotencyDal(options));
            var legacyWriter = new FakeLegacyCompatibilityWriterPort();
            var fakeRead = new FakeLegacyStockReadPort
            {
                Balances = snapshot.Balances,
                JournalEntries = snapshot.Journals
            };

            var claim = new ReconstructionClaimService(spy, scopeRepo);
            var bindingRepo = new StockLayerLegacyBindingRepo(new StockLayerLegacyBindingDal(options));
        var uow = new StockConsequenceUnitOfWork(spy, idempotencyRepo, movementRepo, positionRepo, scopeRepo, legacyWriter, bindingRepo);
            var sut = new ReconstructStockLedgerBaselineHandler(
                claim,
                fakeRead,
                uow,
                spy,
                scopeRepo,
                idempotencyRepo);

            var act = async () => await sut.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Forced position persist failure*");

            // Claim TX already committed — Scope may remain Reconstructing; additive Ledger rows must not.
            CountLayers(key).Should().Be(0);
            CountPositions(key).Should().Be(0);
            CountMovements(key).Should().Be(0);
            idempotencyRepo.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(
                        StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                        BuildIdempotencyKey(key)))
                .HasValue.Should().BeFalse();

            scopeRepo.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructing);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task PhaseB_DoesNotRunInsideWriteTransaction()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("TXB");
        Cleanup(key);

        try
        {
            var snapshot = MultiLocationBalancedSnapshot(key);
            var activeDuringReads = new List<int>();
            SpyUnitOfWork? spy = null;
            var fakeRead = new FakeLegacyStockReadPort
            {
                Balances = snapshot.Balances,
                JournalEntries = snapshot.Journals,
                OnListBalances = () => activeDuringReads.Add(spy!.ActiveScopeCount),
                OnListJournals = () => activeDuringReads.Add(spy!.ActiveScopeCount)
            };
            ReconstructStockLedgerBaselineHandler sut;
            (sut, spy, _, _) = CreateSut(fakeRead);

            var result = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);

            // Phase B is the first balance+journal pair; must observe zero active write scopes.
            activeDuringReads.Should().HaveCountGreaterThanOrEqualTo(2);
            activeDuringReads[0].Should().Be(0);
            activeDuringReads[1].Should().Be(0);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task Reconstruction_DoesNotModifyLegacyStockRows()
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
            var snapshot = MultiLocationBalancedSnapshot(key);
            var (sut, _, legacy, _) = CreateSut(snapshot);

            var result = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);

            conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_stok").Should().Be(stokBefore);
            conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_buku").Should().Be(bukuBefore);
            legacy.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task MultiLocationDo_PersistsPositionsAcrossLocations()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("MLT");
        Cleanup(key);

        try
        {
            var snapshot = MultiLocationBalancedSnapshot(key);
            var (sut, _, _, repos) = CreateSut(snapshot);

            var result = await sut.Handle(new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);

            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);
            CountPositions(key).Should().Be(2);
            CountLayers(key).Should().Be(2);

            var ly01 = repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY01"))
                .Value;
            var ly02 = repos.Position
                .LoadEntity(StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, "LY02"))
                .Value;
            ly01.TotalRemainingQuantity.Should().Be(10m);
            ly02.TotalRemainingQuantity.Should().Be(7m);
            ly01.Layers.Should().OnlyContain(l => l.Origin == StockFactOriginEnum.Reconstructed);
            ly02.Layers.Should().OnlyContain(l => l.Origin == StockFactOriginEnum.Reconstructed);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task BasisChangeDuringPhaseB_SettlesOnRetry_WhenLegacyStabilizes()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("RTY");
        Cleanup(key);

        try
        {
            var stable = MultiLocationBalancedSnapshot(key);
            var transient = stable with
            {
                Balances =
                [
                    Balance(key, "LY01", 9m, 1000m, ExpA, "STK001", T1, "B1"),
                    Balance(key, "LY02", 7m, 1500m, ExpB, "STK002", T2, "B2")
                ]
            };

            // Attempt 1: Phase B = stable, Phase C revalidate = transient → BasisChanged.
            // Attempt 2+: both Phase B and Phase C see stable → Reconstructed.
            var fakeRead = new FakeLegacyStockReadPort
            {
                BalancesByCall = call => call switch
                {
                    1 => stable.Balances,
                    2 => transient.Balances,
                    _ => stable.Balances
                },
                JournalsByCall = call => call switch
                {
                    1 => stable.Journals,
                    2 => transient.Journals,
                    _ => stable.Journals
                }
            };

            var (sut, _, _, repos) = CreateSut(fakeRead);

            var result = await sut.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);
            repos.Scope.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructed);
            CountLayers(key).Should().Be(2);
            CountMovements(key).Should().Be(1);
            fakeRead.BalanceCallCount.Should().BeGreaterThanOrEqualTo(4);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task ConcurrentReconstructors_SingleCommittedBaseline()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("CON");
        Cleanup(key);

        try
        {
            var snapshot = MultiLocationBalancedSnapshot(key);
            var (sut1, _, _, repos) = CreateSut(snapshot);
            var (sut2, _, _, _) = CreateSut(snapshot);
            var command = new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId);

            var outcomes = await Task.WhenAll(
                RunReconstructionToleratingDeadlock(sut1, command),
                RunReconstructionToleratingDeadlock(sut2, command));

            // At least one attempt must commit or observe the committed baseline.
            // The other may lose a SQL deadlock on overlapping Phase C writers — durable
            // state below is the coexistence invariant (no duplicate quantities/layers).
            outcomes.Should().Contain(o =>
                o == ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed
                || o == ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed);

            // After the race, a follow-up request must leave exactly one durable baseline
            // (AlreadyReconstructed if a winner committed; Reconstructed if both deadlocked
            // before Phase C commit and Scope remained Reconstructing).
            var followUp = await sut2.Handle(command, default);
            followUp.Outcome.Should().BeOneOf(
                ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed,
                ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed);

            repos.Scope.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructed);
            CountLayers(key).Should().Be(2);
            CountPositions(key).Should().Be(2);
            CountMovements(key).Should().Be(1);
            repos.Idempotency.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(
                        StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                        BuildIdempotencyKey(key)))
                .HasValue.Should().BeTrue();
        }
        finally
        {
            Cleanup(key);
        }
    }

    /// <summary>
    /// Concurrent Phase C writers may deadlock on disposable SQL Server (FQ-06 interim).
    /// Capture the outcome when possible; null means this attempt was deadlock-victimized.
    /// </summary>
    private static async Task<ReconstructStockLedgerBaselineOutcomeEnum?> RunReconstructionToleratingDeadlock(
        ReconstructStockLedgerBaselineHandler sut,
        ReconstructStockLedgerBaselineCommand command)
    {
        try
        {
            var result = await sut.Handle(command, default);
            return result.Outcome;
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            return null;
        }
    }

    [Fact]
    public async Task PhaseBReadFailure_DoesNotPersistStaleBaseline_LeavesRecoverableClaim()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("TMO");
        Cleanup(key);

        try
        {
            var fakeRead = new FakeLegacyStockReadPort
            {
                ThrowOnNextRead = new TimeoutException(
                    "P2-S8 stand-in: bounded-query / large-history read timeout.")
            };
            var (sut, _, _, repos) = CreateSut(fakeRead);

            var act = () => sut.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            await act.Should().ThrowAsync<TimeoutException>();

            repos.Scope.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructing);
            CountLayers(key).Should().Be(0);
            CountPositions(key).Should().Be(0);
            CountMovements(key).Should().Be(0);
            repos.Idempotency.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(
                        StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                        BuildIdempotencyKey(key)))
                .HasValue.Should().BeFalse();
        }
        finally
        {
            Cleanup(key);
        }
    }

    private static (
        ReconstructStockLedgerBaselineHandler Sut,
        SpyUnitOfWork Spy,
        FakeLegacyCompatibilityWriterPort Legacy,
        Repos Repos)
        CreateSut(
            Snapshot snapshot,
            Action<FakeLegacyStockReadPort>? configureRead = null)
    {
        var fakeRead = new FakeLegacyStockReadPort
        {
            Balances = snapshot.Balances,
            JournalEntries = snapshot.Journals
        };
        configureRead?.Invoke(fakeRead);
        return CreateSut(fakeRead);
    }

    private static (
        ReconstructStockLedgerBaselineHandler Sut,
        SpyUnitOfWork Spy,
        FakeLegacyCompatibilityWriterPort Legacy,
        Repos Repos)
        CreateSut(FakeLegacyStockReadPort fakeRead)
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

        var claim = new ReconstructionClaimService(spy, scopeRepo);
        var bindingRepo = new StockLayerLegacyBindingRepo(new StockLayerLegacyBindingDal(options));
        var uow = new StockConsequenceUnitOfWork(spy, idempotencyRepo, movementRepo, positionRepo, scopeRepo, legacyWriter, bindingRepo);
        var sut = new ReconstructStockLedgerBaselineHandler(
            claim,
            fakeRead,
            uow,
            spy,
            scopeRepo,
            idempotencyRepo);

        return (sut, spy, legacyWriter, new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo));
    }

    private static Snapshot MultiLocationBalancedSnapshot(IStockLedgerScopeKey key)
        => new(
            [
                Balance(key, "LY01", 10m, 1000m, ExpA, "STK001", T1, "B1"),
                Balance(key, "LY02", 7m, 1500m, ExpB, "STK002", T2, "B2")
            ],
            [
                Journal(key, "TRS001", "LY01", qtyIn: 10m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
                Journal(key, "TRS002", "LY02", qtyIn: 10m, qtyOut: 3m, 1500m, ExpB, T2, "B2")
            ]);

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
            $"BRGS6{shortTag}{ulid}"[..13],
            $"DOS6{shortTag}{ulid}"[..10]);
    }

    private static string BuildIdempotencyKey(IStockLedgerScopeKey key)
        => $"RECON|{key.BrgId}|{key.ReceiptSourceId}|baseline";

    private static void Cleanup(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var movementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key);
        var idempotencyKey = BuildIdempotencyKey(key);
        conn.Execute(
            """
            DELETE FROM BILRG_StokMovementLine WHERE StockMovementId = @MovementId;
            DELETE FROM BILRG_StokMovement WHERE StockMovementId = @MovementId;
            DELETE FROM BILRG_StokLayerLegacyBinding WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokLayer WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokPosition WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokLedgerScope WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokSourceIdempotency
            WHERE IdempotencyKind = @Kind AND IdempotencyKey = @IdempotencyKey;
            """,
            new
            {
                MovementId = movementId,
                key.BrgId,
                key.ReceiptSourceId,
                Kind = (int)StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                IdempotencyKey = idempotencyKey
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

    private static int CountPositions(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokPosition
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
            """,
            new { key.BrgId, key.ReceiptSourceId });
    }

    private static int CountMovements(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM BILRG_StokMovement WHERE StockMovementId = @MovementId",
            new { MovementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key) });
    }

    private sealed record Snapshot(
        IReadOnlyList<LegacyStockBalanceType> Balances,
        IReadOnlyList<LegacyStockJournalEntryType> Journals);

    private sealed record Repos(
        IStockLedgerScopeStateRepo Scope,
        IStockMovementRepo Movement,
        IStockPositionRepo Position,
        IStockSourceIdempotencyRepo Idempotency);

}
