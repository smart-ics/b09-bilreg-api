using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.InventoryContext.StockLedgerFeature.Fakes;
using Dapper;
using FluentAssertions;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P5-S1 — Trusted Availability → Freshness → FIFO orchestration.
/// Mix of disposable-DB integration paths and unit fakes for fail-closed outcomes.
/// Orchestrator must not write FO stock (no Legacy Compatibility Writer Apply).
/// </summary>
[Collection("StockLedgerP3S4")]
public class TrustedStockAllocationOrchestratorTest
{
    private static readonly DateTime TEarly = new(2024, 1, 10, 8, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime TLate = new(2024, 2, 15, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime TMid = new(2024, 1, 20, 10, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);
    private static readonly DateOnly ExpB = new(2026, 6, 30);
    private const string LocationId = "LY01";

    [Fact]
    public async Task MultiDoCandidates_SelectsFifoOrderAfterFreshness()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var item = NewBrgId("MD");
        var doEarly = NewDoId("EA");
        var doLate = NewDoId("LA");
        var keyEarly = StockLedgerScopeKeyType.Create(item, doEarly);
        var keyLate = StockLedgerScopeKeyType.Create(item, doLate);
        Cleanup(keyEarly);
        Cleanup(keyLate);

        try
        {
            var balances = new[]
            {
                Balance(keyEarly, LocationId, 5m, 1000m, ExpA, "ST-EA", TEarly, "B1"),
                Balance(keyLate, LocationId, 8m, 1100m, ExpA, "ST-LA", TLate, "B2")
            };
            var journals = new[]
            {
                Journal(keyEarly, "TRS-EA", LocationId, 5m, 0m, 1000m, ExpA, TEarly, "B1"),
                Journal(keyLate, "TRS-LA", LocationId, 8m, 0m, 1100m, ExpA, TLate, "B2")
            };

            var harness = CreateHarness(balances, journals);
            await ReconstructAsync(harness, keyEarly);
            await ReconstructAsync(harness, keyLate);
            await SyncAsync(harness, keyEarly);
            await SyncAsync(harness, keyLate);

            var availability = new FakeAvailabilityDiscoveryPort
            {
                Result = new AvailabilityDiscoveryResult(
                    AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
                    [
                        new AvailabilityCandidateType(doLate, LocationId, 8m, ExpA, "B2"),
                        new AvailabilityCandidateType(doEarly, LocationId, 5m, ExpA, "B1")
                    ],
                    "provisional multi-DO")
            };

            var sut = CreateOrchestrator(harness, availability);
            var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
                BrgObatType.Key(item),
                LayananType.Key(LocationId),
                RequestedQuantity: 7m));

            result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.PlanReady);
            result.AllocationPlan.Should().NotBeNull();
            result.AllocationPlan!.IsFulfilled.Should().BeTrue();
            result.AllocationPlan.Allocations.Should().HaveCount(2);
            result.AllocationPlan.Allocations[0].ReceiptSourceId.Should().Be(doEarly);
            result.AllocationPlan.Allocations[0].Quantity.Should().Be(5m);
            result.AllocationPlan.Allocations[1].ReceiptSourceId.Should().Be(doLate);
            result.AllocationPlan.Allocations[1].Quantity.Should().Be(2m);
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(keyEarly);
            Cleanup(keyLate);
        }
    }

    [Fact]
    public async Task ExplicitExpirationDateFilter_AllocatesMatchingEdOnly()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var item = NewBrgId("ED");
        var doA = NewDoId("A");
        var doB = NewDoId("B");
        var keyA = StockLedgerScopeKeyType.Create(item, doA);
        var keyB = StockLedgerScopeKeyType.Create(item, doB);
        Cleanup(keyA);
        Cleanup(keyB);

        try
        {
            var balances = new[]
            {
                Balance(keyA, LocationId, 6m, 1000m, ExpA, "ST-A", TEarly, "BA"),
                Balance(keyB, LocationId, 6m, 1000m, ExpB, "ST-B", TMid, "BB")
            };
            var journals = new[]
            {
                Journal(keyA, "TRS-A", LocationId, 6m, 0m, 1000m, ExpA, TEarly, "BA"),
                Journal(keyB, "TRS-B", LocationId, 6m, 0m, 1000m, ExpB, TMid, "BB")
            };

            var harness = CreateHarness(balances, journals);
            await ReconstructAsync(harness, keyA);
            await ReconstructAsync(harness, keyB);
            await SyncAsync(harness, keyA);
            await SyncAsync(harness, keyB);

            var availability = new FakeAvailabilityDiscoveryPort
            {
                Result = new AvailabilityDiscoveryResult(
                    AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
                    [new AvailabilityCandidateType(doA, LocationId, 6m, ExpA, "BA")],
                    "ED-filtered provisional")
            };

            var sut = CreateOrchestrator(harness, availability);
            var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
                BrgObatType.Key(item),
                LayananType.Key(LocationId),
                RequestedQuantity: 4m,
                ExpirationDateFilter: ExpA));

            result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.PlanReady);
            result.AllocationPlan!.Allocations.Should().OnlyContain(a => a.ExpirationDate == ExpA);
            result.AllocationPlan.Allocations.Should().OnlyContain(a => a.ReceiptSourceId == doA);
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(keyA);
            Cleanup(keyB);
        }
    }

    [Fact]
    public async Task InsufficientAuthoritativeStock_ReturnsBeforeGate()
    {
        var availability = new FakeAvailabilityDiscoveryPort
        {
            Result = new AvailabilityDiscoveryResult(
                AvailabilityDiscoveryOutcomeEnum.InsufficientAuthoritativeStock,
                Array.Empty<AvailabilityCandidateType>(),
                "no tb_stok rows")
        };

        var reconstructCalls = 0;
        var (gate, tracker) = CreateAlwaysCurrentGate(DummyScope("BRG-INSUF", "DO-INSUF"));
        var sut = new TrustedStockAllocationOrchestrator(
            availability,
            new EmptyScopeStateRepo(),
            new EmptyPositionRepo(),
            gate,
            (_, _) =>
            {
                reconstructCalls++;
                throw new InvalidOperationException("Reconstruct must not run when Availability is insufficient.");
            });

        var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
            BrgObatType.Key("BRG-INSUF"),
            LayananType.Key(LocationId),
            RequestedQuantity: 5m));

        result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.InsufficientAuthoritativeStock);
        result.AllocationPlan.Should().BeNull();
        reconstructCalls.Should().Be(0);
        tracker.Calls.Should().Be(0);
    }

    [Fact]
    public async Task InsufficientLedgerStock_AfterFreshness_FailClosed()
    {
        var item = "BRG-LEDGER";
        var doId = "DO-LEDGER";
        var scopeKey = StockLedgerScopeKeyType.Create(item, doId);
        var position = SynchronizationPositionType.CreateFromUtf8Token(
            "unit-fp",
            LegacyReconstructionBasisCalculator.AlgorithmVersion);
        var scope = StockLedgerScopeStateModel.Rehydrate(
            item,
            doId,
            ReconstructionStatusEnum.Reconstructed,
            SynchronizationStateEnum.Current,
            position,
            LegacyReconstructionBasisCalculator.AlgorithmVersion,
            inconsistencyReason: null);

        var availability = new FakeAvailabilityDiscoveryPort
        {
            Result = new AvailabilityDiscoveryResult(
                AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
                [new AvailabilityCandidateType(doId, LocationId, 10m, ExpA, "B")],
                "legacy says 10")
        };

        var (gate, tracker) = CreateAlwaysCurrentGate(scope);
        var sut = new TrustedStockAllocationOrchestrator(
            availability,
            new StubScopeStateRepo(scope),
            new StubPositionRepo(CreatePositionWithQty(item, doId, LocationId, remaining: 2m)),
            gate,
            (_, _) => throw new InvalidOperationException("Already reconstructed."));

        var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
            BrgObatType.Key(item),
            LayananType.Key(LocationId),
            RequestedQuantity: 10m));

        result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.InsufficientLedgerStock);
        result.AllocationPlan.Should().BeNull();
        result.Explanation.Should().Contain("available 2");
        tracker.Calls.Should().Be(1);
        _ = scopeKey;
    }

    [Fact]
    public async Task StaleScope_FailClosed()
    {
        var item = "BRG-STALE";
        var doId = "DO-STALE";
        var scopeKey = StockLedgerScopeKeyType.Create(item, doId);
        var position = SynchronizationPositionType.CreateFromUtf8Token(
            "stale-fp",
            LegacyReconstructionBasisCalculator.AlgorithmVersion);
        var scope = StockLedgerScopeStateModel.Rehydrate(
            item,
            doId,
            ReconstructionStatusEnum.Reconstructed,
            SynchronizationStateEnum.SynchronizationRequired,
            position,
            LegacyReconstructionBasisCalculator.AlgorithmVersion,
            inconsistencyReason: null);

        var availability = new FakeAvailabilityDiscoveryPort
        {
            Result = new AvailabilityDiscoveryResult(
                AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
                [new AvailabilityCandidateType(doId, LocationId, 5m, ExpA, null)],
                "provisional")
        };

        var gate = new LegacyStockFreshnessGate(
            new StubScopeStateRepo(scope),
            new FakeLegacyChangeDiscoveryPort
            {
                DiscoveryResult = new LegacyChangeDiscoveryResult(
                    LegacyChangeDiscoveryOutcomeEnum.Undeterminable,
                    CurrentFingerprint: null,
                    Deltas: Array.Empty<LegacyDiscoveredDeltaType>(),
                    Explanation: "undeterminable freshness for unit test")
            },
            new FakeLegacyStockReadPort(),
            new StubIdempotencyRepo(),
            (_, _) => throw new InvalidOperationException("Sync must not run for Undeterminable."));

        var sut = new TrustedStockAllocationOrchestrator(
            availability,
            new StubScopeStateRepo(scope),
            new EmptyPositionRepo(),
            gate,
            (_, _) => throw new InvalidOperationException("Already reconstructed."));

        var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
            BrgObatType.Key(item),
            LayananType.Key(LocationId),
            RequestedQuantity: 3m));

        result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.StaleOrNotCurrent);
        result.AllocationPlan.Should().BeNull();
        _ = scopeKey;
    }

    [Fact]
    public async Task InconsistentScope_FailClosed()
    {
        var item = "BRG-INC";
        var doId = "DO-INC";
        var position = SynchronizationPositionType.CreateFromUtf8Token(
            "inc-fp",
            LegacyReconstructionBasisCalculator.AlgorithmVersion);
        var scope = StockLedgerScopeStateModel.Rehydrate(
            item,
            doId,
            ReconstructionStatusEnum.Inconsistent,
            SynchronizationStateEnum.Inconsistent,
            position,
            LegacyReconstructionBasisCalculator.AlgorithmVersion,
            inconsistencyReason: "Material drift for P5-S1 unit test.");

        var availability = new FakeAvailabilityDiscoveryPort
        {
            Result = new AvailabilityDiscoveryResult(
                AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
                [new AvailabilityCandidateType(doId, LocationId, 5m, ExpA, null)],
                "provisional")
        };

        var (gate, tracker) = CreateAlwaysCurrentGate(scope);
        var sut = new TrustedStockAllocationOrchestrator(
            availability,
            new StubScopeStateRepo(scope),
            new EmptyPositionRepo(),
            gate,
            (_, _) => throw new InvalidOperationException("Must not reconstruct Inconsistent."));

        var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
            BrgObatType.Key(item),
            LayananType.Key(LocationId),
            RequestedQuantity: 3m));

        result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.Inconsistent);
        result.Explanation.Should().Contain("Material drift");
        tracker.Calls.Should().Be(0);
    }

    [Fact]
    public async Task SynchronizedNow_ThenAllocateSucceeds()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("SYN");
        Cleanup(key);

        try
        {
            var baselineBalances = new[]
            {
                Balance(key, LocationId, 10m, 1000m, ExpA, "STK001", TEarly, "B1")
            };
            var baselineJournals = new[]
            {
                Journal(key, "TRS001", LocationId, 10m, 0m, 1000m, ExpA, TEarly, "B1")
            };

            var harness = CreateHarness(baselineBalances, baselineJournals);
            await ReconstructAsync(harness, key);
            await SyncAsync(harness, key);

            var changedBalances = new[]
            {
                Balance(key, LocationId, 12m, 1000m, ExpA, "STK001", TEarly, "B1")
            };
            var changedJournals = new[]
            {
                Journal(key, "TRS001", LocationId, 10m, 0m, 1000m, ExpA, TEarly, "B1"),
                Journal(key, "TRS-NEW", LocationId, 2m, 0m, 1000m, ExpA, TLate, "B1")
            };
            ConfigureScopedReads(harness.LegacyRead, changedBalances, changedJournals);

            var availability = new FakeAvailabilityDiscoveryPort
            {
                Result = new AvailabilityDiscoveryResult(
                    AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
                    [new AvailabilityCandidateType(key.ReceiptSourceId, LocationId, 12m, ExpA, "B1")],
                    "after legacy insert")
            };

            var sut = CreateOrchestrator(harness, availability);
            var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
                BrgObatType.Key(key.BrgId),
                LayananType.Key(LocationId),
                RequestedQuantity: 11m));

            result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.PlanReady);
            result.AllocationPlan!.AllocatedQuantity.Should().Be(11m);
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task ReconstructWhenNoBaseline_ThenAllocate()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("RCN");
        Cleanup(key);

        try
        {
            var balances = new[]
            {
                Balance(key, LocationId, 9m, 1000m, ExpA, "ST-RCN", TEarly, "B1")
            };
            var journals = new[]
            {
                Journal(key, "TRS-RCN", LocationId, 9m, 0m, 1000m, ExpA, TEarly, "B1")
            };

            var harness = CreateHarness(balances, journals);
            harness.Repos.Scope.LoadEntity(key).HasValue.Should().BeFalse();

            var availability = new FakeAvailabilityDiscoveryPort
            {
                Result = new AvailabilityDiscoveryResult(
                    AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
                    [new AvailabilityCandidateType(key.ReceiptSourceId, LocationId, 9m, ExpA, "B1")],
                    "needs reconstruct")
            };

            var sut = CreateOrchestrator(harness, availability);
            var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
                BrgObatType.Key(key.BrgId),
                LayananType.Key(LocationId),
                RequestedQuantity: 4m));

            result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.PlanReady);
            result.AllocationPlan!.AllocatedQuantity.Should().Be(4m);
            harness.Repos.Scope.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructed);
            harness.LegacyWriter.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task NoLegacyStockMutation_FromOrchestratorAlone()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("NOM");
        Cleanup(key);

        try
        {
            var balances = new[]
            {
                Balance(key, LocationId, 5m, 1000m, ExpA, "ST-NOM", TEarly, "B1")
            };
            var journals = new[]
            {
                Journal(key, "TRS-NOM", LocationId, 5m, 0m, 1000m, ExpA, TEarly, "B1")
            };

            var harness = CreateHarness(balances, journals);
            await ReconstructAsync(harness, key);
            await SyncAsync(harness, key);

            var availability = new FakeAvailabilityDiscoveryPort
            {
                Result = new AvailabilityDiscoveryResult(
                    AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
                    [new AvailabilityCandidateType(key.ReceiptSourceId, LocationId, 5m, ExpA, "B1")],
                    "plan only")
            };

            var sut = CreateOrchestrator(harness, availability);
            var result = await sut.PlanAsync(new TrustedStockAllocationRequest(
                BrgObatType.Key(key.BrgId),
                LayananType.Key(LocationId),
                RequestedQuantity: 3m));

            result.Outcome.Should().Be(TrustedStockAllocationOutcomeEnum.PlanReady);
            harness.LegacyWriter.Applied.Should().BeEmpty(
                "P5-S1 must not invoke Legacy Compatibility Writer / FO stock writes.");

            // Ledger positions remain unchanged (plan is non-mutating).
            var writeScope = StockWriteScopeKeyType.Create(key.BrgId, key.ReceiptSourceId, LocationId);
            harness.Repos.Position.LoadEntity(writeScope).Value.TotalRemainingQuantity.Should().Be(5m);
        }
        finally
        {
            Cleanup(key);
        }
    }

    #region Harness helpers

    private static TrustedStockAllocationOrchestrator CreateOrchestrator(
        Harness harness,
        IAvailabilityDiscoveryPort availability)
    {
        var discovery = new LegacyChangeDiscoveryPort(
            harness.LegacyRead,
            new StockSourceIdempotencyDal(ConnStringHelper.GetTestEnv()));
        var gate = new LegacyStockFreshnessGate(
            harness.Repos.Scope,
            discovery,
            harness.LegacyRead,
            harness.Repos.Idempotency,
            harness.Sync.Handle);

        return new TrustedStockAllocationOrchestrator(
            availability,
            harness.Repos.Scope,
            harness.Repos.Position,
            gate,
            harness.Reconstruct);
    }

    private static async Task ReconstructAsync(Harness harness, IStockLedgerScopeKey key)
    {
        var result = await harness.Reconstruct.Handle(
            new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId), default);
        result.Outcome.Should().BeOneOf(
            ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed,
            ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed);
    }

    private static async Task SyncAsync(Harness harness, IStockLedgerScopeKey key)
    {
        var result = await harness.Sync.Handle(
            new SynchronizeStockLedgerScopeCommand(key.BrgId, key.ReceiptSourceId), default);
        result.Outcome.Should().BeOneOf(
            SynchronizeStockLedgerScopeOutcomeEnum.Synchronized,
            SynchronizeStockLedgerScopeOutcomeEnum.AlreadyCurrent);
    }

    private static Harness CreateHarness(
        IReadOnlyList<LegacyStockBalanceType> balances,
        IReadOnlyList<LegacyStockJournalEntryType> journals)
    {
        var fakeRead = new FakeLegacyStockReadPort();
        ConfigureScopedReads(fakeRead, balances, journals);

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
        var bindingRepo = new StockLayerLegacyBindingRepo(new StockLayerLegacyBindingDal(options));
        var uow = new StockConsequenceUnitOfWork(spy, idempotencyRepo, movementRepo, positionRepo, scopeRepo, legacyWriter, bindingRepo);

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
            fakeRead,
            legacyWriter,
            new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo));
    }

    private static void ConfigureScopedReads(
        FakeLegacyStockReadPort port,
        IReadOnlyList<LegacyStockBalanceType> balances,
        IReadOnlyList<LegacyStockJournalEntryType> journals)
    {
        port.BalancesByCall = _ =>
        {
            var scope = port.BalanceRequests[^1];
            return balances
                .Where(b => b.BrgId == scope.BrgId && b.ReceiptSourceId == scope.ReceiptSourceId)
                .ToArray();
        };
        port.JournalsByCall = _ =>
        {
            var scope = port.JournalRequests[^1];
            return journals
                .Where(j => j.BrgId == scope.BrgId && j.ReceiptSourceId == scope.ReceiptSourceId)
                .ToArray();
        };
    }

    private static StockPositionModel CreatePositionWithQty(
        string brgId,
        string doId,
        string layananId,
        decimal remaining)
    {
        var writeScope = StockWriteScopeKeyType.Create(brgId, doId, layananId);
        var layer = StockLayerModel.Create(
            BrgObatType.Key(brgId),
            ReceiptSourceType.Create(doId),
            LayananType.Key(layananId),
            StockMovementModel.Key("MOV-UNIT"),
            initialQuantity: remaining,
            UnitValuationType.Create(1000m),
            TEarly,
            StockFactOriginEnum.Reconstructed,
            expirationDate: ExpA,
            stockLayerId: "LYR-UNIT");
        return StockPositionModel.Create(writeScope, [layer]);
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

    private static string NewBrgId(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var tag2 = tag.Length <= 2 ? tag : tag[..2];
        return (tag2 + ulid)[..13];
    }

    private static string NewDoId(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var tag2 = tag.Length <= 2 ? tag : tag[..2];
        return (tag2 + ulid)[2..12];
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
            DELETE FROM BILRG_StokLayerLegacyBinding WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
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

    private static StockLedgerScopeStateModel DummyScope(string brgId, string doId)
    {
        var position = SynchronizationPositionType.CreateFromUtf8Token(
            "dummy-fp",
            LegacyReconstructionBasisCalculator.AlgorithmVersion);
        return StockLedgerScopeStateModel.Rehydrate(
            brgId,
            doId,
            ReconstructionStatusEnum.Reconstructed,
            SynchronizationStateEnum.Current,
            position,
            LegacyReconstructionBasisCalculator.AlgorithmVersion,
            inconsistencyReason: null);
    }

    private static (LegacyStockFreshnessGate Gate, GateCallTracker Tracker) CreateAlwaysCurrentGate(
        StockLedgerScopeStateModel scope)
    {
        var tracker = new GateCallTracker();
        var discovery = new CountingDiscoveryPort(
            tracker,
            new FakeLegacyChangeDiscoveryPort
            {
                FingerprintToReturn = scope.SynchronizationPosition,
                DiscoveryResult = new LegacyChangeDiscoveryResult(
                    LegacyChangeDiscoveryOutcomeEnum.Unchanged,
                    scope.SynchronizationPosition,
                    Array.Empty<LegacyDiscoveredDeltaType>(),
                    "unit: unchanged")
            });

        // Empty legacy snapshot ⇒ coverage complete; Unchanged ⇒ Current without sync.
        var gate = new LegacyStockFreshnessGate(
            new StubScopeStateRepo(scope),
            discovery,
            new FakeLegacyStockReadPort(),
            new CompleteCoverageIdempotencyRepo(),
            (_, _) => throw new InvalidOperationException("Sync must not run on Current fast-path."));

        return (gate, tracker);
    }

    private sealed class GateCallTracker
    {
        public int Calls { get; set; }
    }

    private sealed class CountingDiscoveryPort : ILegacyChangeDiscoveryPort
    {
        private readonly GateCallTracker _tracker;
        private readonly ILegacyChangeDiscoveryPort _inner;

        public CountingDiscoveryPort(GateCallTracker tracker, ILegacyChangeDiscoveryPort inner)
        {
            _tracker = tracker;
            _inner = inner;
        }

        public SynchronizationPositionType ComputeCurrentFingerprint(IStockLedgerScopeKey scope)
            => _inner.ComputeCurrentFingerprint(scope);

        public LegacyChangeDiscoveryResult DiscoverChanges(
            IStockLedgerScopeKey scope,
            SynchronizationPositionType? storedPosition)
        {
            _tracker.Calls++;
            return _inner.DiscoverChanges(scope, storedPosition);
        }
    }

    private sealed class CompleteCoverageIdempotencyRepo : IStockSourceIdempotencyRepo
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

    private sealed class EmptyScopeStateRepo : IStockLedgerScopeStateRepo
    {
        public void SaveChanges(StockLedgerScopeStateModel model) { }

        public MayBe<StockLedgerScopeStateModel> LoadEntity(IStockLedgerScopeKey id)
            => MayBe<StockLedgerScopeStateModel>.None;

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

    private sealed class StubPositionRepo : IStockPositionRepo
    {
        private readonly StockPositionModel _position;

        public StubPositionRepo(StockPositionModel position) => _position = position;

        public void SaveChanges(StockPositionModel model)
            => throw new InvalidOperationException("P5-S1 must not persist positions.");

        public MayBe<StockPositionModel> LoadEntity(IStockWriteScopeKey key)
            => MayBe<StockPositionModel>.Some(_position);

        public IReadOnlyList<StockPositionModel> ListByLedgerScope(IStockLedgerScopeKey scope)
            => [_position];
    }

    private sealed class EmptyPositionRepo : IStockPositionRepo
    {
        public void SaveChanges(StockPositionModel model)
            => throw new InvalidOperationException("P5-S1 must not persist positions.");

        public MayBe<StockPositionModel> LoadEntity(IStockWriteScopeKey key)
            => MayBe<StockPositionModel>.None;

        public IReadOnlyList<StockPositionModel> ListByLedgerScope(IStockLedgerScopeKey scope)
            => Array.Empty<StockPositionModel>();
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

    #endregion
}
