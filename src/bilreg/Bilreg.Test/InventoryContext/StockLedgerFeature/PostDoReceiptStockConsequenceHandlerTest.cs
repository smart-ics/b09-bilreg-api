using System.Data.SqlClient;
using System.Reflection;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.InventoryContext.StockLedgerFeature.Fakes;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S2 — Native DO Receipt consequence UseCase against disposable <c>devTest</c>.
/// Never writes <c>HOSPITAL_HPL</c>.
/// </summary>
public class PostDoReceiptStockConsequenceHandlerTest
{
    private static readonly DateTime BusinessTime = new(2026, 8, 8, 11, 0, 0);
    private static readonly DateTime ProcessedAt = new(2026, 8, 8, 11, 5, 0);

    [Fact]
    public async Task HappyPath_CapabilityEnabled_NativeOriginAndLegacyRows()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("HP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);
            result.StockMovementId.Should().NotBeNullOrWhiteSpace();
            result.ScopeState!.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            result.SynchronizationPosition!.AlgorithmVersion.Should().Be(
                LegacyReconstructionBasisCalculator.AlgorithmVersion);

            var movement = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(result.StockMovementId!));
            movement.HasValue.Should().BeTrue();
            movement.Value.Origin.Should().Be(StockFactOriginEnum.Native);
            movement.Value.Lines.Should().OnlyContain(l => l.Origin == StockFactOriginEnum.Native);

            var position = harness.Repos.Position.LoadEntity(
                StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.LocationId));
            position.HasValue.Should().BeTrue();
            position.Value.Layers.Should().ContainSingle();
            position.Value.Layers[0].Origin.Should().Be(StockFactOriginEnum.Native);
            position.Value.Layers[0].RemainingQuantity.Should().Be(10m);

            var balances = harness.LegacyRead.ListCurrentBalances(ids.Scope);
            var journals = harness.LegacyRead.ListJournalEntries(ids.Scope);
            balances.Should().ContainSingle();
            balances[0].Quantity.Should().Be(10m);
            balances[0].UnitCost.Should().Be(1500.50m);
            balances[0].ReceiptSourceId.Should().Be(ids.DoId);
            balances[0].LayananId.Should().Be(ids.LocationId);
            journals.Should().ContainSingle();
            journals[0].MutationKindId.Should().Be("DO");
            journals[0].QuantityIn.Should().Be(10m);

            CountSyncIdentityKeys(ids.Scope).Should().Be(2);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task CapabilityDisabled_NoLedgerOrLegacyWrites()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("OFF");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: false);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Disabled);
            result.StockMovementId.Should().BeNull();

            CountLedgerMovements(ids).Should().Be(0);
            CountLegacyRows(ids).Should().Be((0, 0));
            harness.Repos.Scope.LoadEntity(ids.Scope).HasValue.Should().BeFalse();
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task DuplicateSourceTransaction_AlreadyCommitted_QuantityNeutral()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("DUP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var first = await harness.Handler.Handle(BuildCommand(ids), default);
            first.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);

            var second = await harness.Handler.Handle(BuildCommand(ids), default);
            second.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.AlreadyCommitted);
            second.StockMovementId.Should().Be(first.StockMovementId);

            CountLedgerMovements(ids).Should().Be(1);
            CountLegacyRows(ids).Should().Be((1, 1));
            harness.LegacyRead.ListCurrentBalances(ids.Scope).Should().ContainSingle()
                .Which.Quantity.Should().Be(10m);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task ScopePosition_UsesFingerprintV1()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("FP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);
            result.SynchronizationPosition!.AlgorithmVersion.Should().Be("fingerprint-v1");

            var balances = harness.LegacyRead.ListCurrentBalances(ids.Scope);
            var journals = harness.LegacyRead.ListJournalEntries(ids.Scope);
            var expected = LegacyReconstructionBasisCalculator.Compute(balances, journals);
            result.SynchronizationPosition.Should().Be(expected);

            var scope = harness.Repos.Scope.LoadEntity(ids.Scope).Value;
            scope.ReconstructionBasisVersion.Should().Be(
                LegacyReconstructionBasisCalculator.AlgorithmVersion);
            scope.SynchronizationPosition.Should().Be(expected);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task NoIsAuthoritative_OnScopeOrMovement()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("AUTH");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);
            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);

            foreach (var type in new[]
                     {
                         typeof(StockLedgerScopeStateModel),
                         typeof(StockMovementModel),
                         typeof(SynchronizationPositionType)
                     })
            {
                type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(m => m.Name)
                    .Should()
                    .NotContain(name => name.Contains("Authoritative", StringComparison.OrdinalIgnoreCase),
                        because: $"{type.Name} must not encode authority");
            }
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task ReconstructedStaleScope_FailsClosedViaFreshnessGate()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("ST");
        Cleanup(ids);

        try
        {
            var priorPosition = SynchronizationPositionType.CreateFromUtf8Token(
                "prior-fp",
                LegacyReconstructionBasisCalculator.AlgorithmVersion);
            var reconstructed = StockLedgerScopeStateModel
                .CreateNotReconstructed(ids.Scope)
                .EstablishFromNativeReceipt(
                    priorPosition,
                    LegacyReconstructionBasisCalculator.AlgorithmVersion);

            var options = ConnStringHelper.GetTestEnv();
            var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
            scopeRepo.SaveChanges(reconstructed);

            var discovery = new FakeLegacyChangeDiscoveryPort
            {
                DiscoveryResult = new LegacyChangeDiscoveryResult(
                    LegacyChangeDiscoveryOutcomeEnum.Undeterminable,
                    CurrentFingerprint: null,
                    Deltas: Array.Empty<LegacyDiscoveredDeltaType>(),
                    Explanation: "Fake: undeterminable for P4-S2 stale fail-closed.")
            };

            var harness = CreateLiveHarness(enabled: true, discoveryOverride: discovery);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.StaleOrNotCurrent);
            result.StockMovementId.Should().BeNull();
            CountLedgerMovements(ids).Should().Be(0);
            CountLegacyRows(ids).Should().Be((0, 0));
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task PriorLegacyHistory_NotReconstructed_FailsClosed_NoMutation()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("PL");
        Cleanup(ids);

        try
        {
            SeedPriorLegacyReceipt(ids, quantity: 7m);

            var beforeLegacy = CountLegacyRows(ids);
            beforeLegacy.Should().Be((1, 1));
            var beforeQty = CreateLiveHarness(enabled: true).LegacyRead
                .ListCurrentBalances(ids.Scope).Single().Quantity;

            var harness = CreateLiveHarness(enabled: true);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.ScopeNotEligible);
            result.StockMovementId.Should().BeNull();
            result.Explanation.Should().Contain("prior legacy");

            CountLegacyRows(ids).Should().Be(beforeLegacy);
            harness.LegacyRead.ListCurrentBalances(ids.Scope).Should().ContainSingle()
                .Which.Quantity.Should().Be(beforeQty);
            CountLedgerMovements(ids).Should().Be(0);
            harness.Repos.Scope.LoadEntity(ids.Scope).HasValue.Should().BeFalse();
            CountSyncIdentityKeys(ids.Scope).Should().Be(0);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task PriorLedgerPosition_NotReconstructed_FailsClosed_NoMutation()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("PP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            SeedPriorLedgerPosition(harness.Repos.Position, ids);

            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.ScopeNotEligible);
            result.StockMovementId.Should().BeNull();
            result.Explanation.Should().Contain("Ledger");

            CountLegacyRows(ids).Should().Be((0, 0));
            CountLedgerMovements(ids).Should().Be(0);
            harness.Repos.Scope.LoadEntity(ids.Scope).HasValue.Should().BeFalse();
            harness.Repos.Position.ListByLedgerScope(ids.Scope).Should().ContainSingle();
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task MultiLine_HappyPath_Committed()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("ML");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var result = await harness.Handler.Handle(BuildCommand(ids, lineCount: 2), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);

            var movement = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(result.StockMovementId!));
            movement.HasValue.Should().BeTrue();
            movement.Value.Lines.Should().HaveCount(2);
            movement.Value.Lines.Should().OnlyContain(l => l.Origin == StockFactOriginEnum.Native);

            CountLegacyRows(ids).Should().Be((2, 2));
            harness.LegacyRead.ListCurrentBalances(ids.Scope).Should().HaveCount(2);
            harness.Repos.Position.ListByLedgerScope(ids.Scope).Should().HaveCount(2);
            CountSyncIdentityKeys(ids.Scope).Should().Be(4);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task ReconstructedCurrent_ScopeNotEligible()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("RC");
        Cleanup(ids);

        try
        {
            var fingerprint = SynchronizationPositionType.CreateFromUtf8Token(
                "current-fp",
                LegacyReconstructionBasisCalculator.AlgorithmVersion);
            var reconstructed = StockLedgerScopeStateModel
                .CreateNotReconstructed(ids.Scope)
                .EstablishFromNativeReceipt(
                    fingerprint,
                    LegacyReconstructionBasisCalculator.AlgorithmVersion);

            var options = ConnStringHelper.GetTestEnv();
            new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options))
                .SaveChanges(reconstructed);

            // Empty legacy authority ⇒ identity coverage vacuously complete;
            // Unchanged discovery ⇒ Freshness Gate returns Current.
            var discovery = new FakeLegacyChangeDiscoveryPort
            {
                DiscoveryResult = new LegacyChangeDiscoveryResult(
                    LegacyChangeDiscoveryOutcomeEnum.Unchanged,
                    CurrentFingerprint: fingerprint,
                    Deltas: Array.Empty<LegacyDiscoveredDeltaType>(),
                    Explanation: "Fake: unchanged for P4-S2 Current → ScopeNotEligible.")
            };

            var harness = CreateLiveHarness(enabled: true, discoveryOverride: discovery);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.ScopeNotEligible);
            result.StockMovementId.Should().BeNull();
            result.Explanation.Should().Contain("already Reconstructed");
            CountLedgerMovements(ids).Should().Be(0);
            CountLegacyRows(ids).Should().Be((0, 0));
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task InconsistentScope_FailsClosed()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("IN");
        Cleanup(ids);

        try
        {
            var inconsistent = StockLedgerScopeStateModel
                .CreateNotReconstructed(ids.Scope)
                .RequireReconstruction()
                .BeginReconstruction()
                .MarkReconstructionInconsistent("Seeded inconsistent for P4-S2.");

            var options = ConnStringHelper.GetTestEnv();
            new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options))
                .SaveChanges(inconsistent);

            var harness = CreateLiveHarness(enabled: true);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Inconsistent);
            result.StockMovementId.Should().BeNull();
            CountLedgerMovements(ids).Should().Be(0);
            CountLegacyRows(ids).Should().Be((0, 0));
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task ReconstructionRequired_ScopeNotEligible()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("RR");
        Cleanup(ids);

        try
        {
            var required = StockLedgerScopeStateModel
                .CreateNotReconstructed(ids.Scope)
                .RequireReconstruction();

            var options = ConnStringHelper.GetTestEnv();
            new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options))
                .SaveChanges(required);

            var harness = CreateLiveHarness(enabled: true);
            var result = await harness.Handler.Handle(BuildCommand(ids), default);

            result.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.ScopeNotEligible);
            result.StockMovementId.Should().BeNull();
            result.Explanation.Should().Contain("ReconstructionRequired");
            CountLedgerMovements(ids).Should().Be(0);
            CountLegacyRows(ids).Should().Be((0, 0));
        }
        finally
        {
            Cleanup(ids);
        }
    }

    private static PostDoReceiptStockConsequenceCommand BuildCommand(CaseIds ids, int lineCount = 1)
    {
        var lines = new List<DoReceiptLineFact>(lineCount);
        for (var i = 0; i < lineCount; i++)
        {
            lines.Add(new DoReceiptLineFact(
                LineNumber: i + 1,
                LayananId: i == 0 ? ids.LocationId : ids.LocationId2,
                Quantity: 10m + i,
                UnitCost: 1500.50m,
                ExpirationDate: new DateOnly(2027, 6, 30),
                Batch: "P4S2-BATCH",
                PurchaseOrderId: ids.PoId,
                SmallestUnitId: "TAB"));
        }

        return new(
            ids.BrgId,
            ids.DoId,
            ids.SourceTxId,
            BusinessTime,
            lines,
            ProcessedAt);
    }

    private static Harness CreateLiveHarness(
        bool enabled,
        FakeLegacyChangeDiscoveryPort? discoveryOverride = null)
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

        var discovery = discoveryOverride ?? new FakeLegacyChangeDiscoveryPort
        {
            DiscoveryResult = new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.Unchanged,
                CurrentFingerprint: SynchronizationPositionType.CreateFromUtf8Token(
                    "unused",
                    LegacyReconstructionBasisCalculator.AlgorithmVersion),
                Deltas: Array.Empty<LegacyDiscoveredDeltaType>(),
                Explanation: "Fake default Unchanged.")
        };

        // Gate is only exercised for already-Reconstructed scopes in P4-S2.
        // Undeterminable / stale paths never invoke sync; keep a no-op delegate.
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

        return new Harness(
            handler,
            legacyRead,
            new Repos(movementRepo, positionRepo, scopeRepo, idempotencyRepo));
    }

    private static void SeedPriorLegacyReceipt(CaseIds ids, decimal quantity)
    {
        var writer = new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv());
        var request = new LegacyCompatibilityWriteRequest(
            SourceTransactionReferenceType.Key($"PRIOR-{ids.SourceTxId}"),
            StockMovementKindEnum.Receipt,
            ids.Scope,
            [
                new LegacyCompatibilityBalanceMutationType(
                    LegacyBalanceMutationActionEnum.Upsert,
                    ids.BrgId,
                    ids.DoId,
                    ids.LocationId,
                    Quantity: quantity,
                    UnitCost: 900m,
                    ExpirationDate: new DateOnly(2027, 1, 15),
                    Batch: "PRIOR-BATCH",
                    PurchaseOrderId: ids.PoId,
                    LegacyRowId: null,
                    SmallestUnitId: "TAB")
            ],
            [
                new LegacyCompatibilityJournalEntryType(
                    LegacyJournalId: string.Empty,
                    BrgId: ids.BrgId,
                    ReceiptSourceId: ids.DoId,
                    LayananId: ids.LocationId,
                    QuantityIn: quantity,
                    QuantityOut: 0m,
                    UnitCost: 900m,
                    ExpirationDate: new DateOnly(2027, 1, 15),
                    Batch: "PRIOR-BATCH",
                    MutationKindId: "DO",
                    MutationTransactionId: ids.DoId,
                    MutationTime: BusinessTime.AddHours(-2),
                    IsVoid: false,
                    SmallestUnitId: "TAB")
            ]);

        using var trans = TransHelper.NewScope();
        writer.Apply(request);
        trans.Complete();
    }

    private static void SeedPriorLedgerPosition(IStockPositionRepo positionRepo, CaseIds ids)
    {
        var item = new BrgReff(ids.BrgId, ids.BrgId);
        var receiptSource = ReceiptSourceType.Key(ids.DoId);
        var location = LayananType.Key(ids.LocationId);
        var layer = StockLayerModel.Create(
            item,
            receiptSource,
            location,
            StockMovementModel.Key(Ulid.NewUlid().ToString()),
            initialQuantity: 3m,
            UnitValuationType.Create(500m),
            BusinessTime.AddDays(-1),
            StockFactOriginEnum.Reconstructed,
            expirationDate: new DateOnly(2027, 3, 1),
            batch: "LEDGER-PRIOR");

        var position = StockPositionModel.CreateEmpty(item, receiptSource, location)
            .AddLayer(layer);
        positionRepo.SaveChanges(position);
    }

    private static CaseIds CreateCaseIds(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 2 ? tag : tag[..2];
        var brgId = $"BRG{shortTag}{ulid}"[..13];
        var doId = $"DO{shortTag}{ulid}"[..10];
        return new CaseIds(
            SourceTxId: $"P4S2-{tag}-{ulid}",
            BrgId: brgId,
            DoId: doId,
            PoId: $"PO{shortTag}{ulid}"[..10],
            LocationId: $"G{shortTag}{ulid}"[..5],
            LocationId2: $"H{shortTag}{ulid}"[..5],
            Scope: StockLedgerScopeKeyType.Create(brgId, doId));
    }

    private static (int Stok, int Buku) CountLegacyRows(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var stok = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM tb_stok WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId",
            new { ids.BrgId, ids.DoId });
        var buku = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM tb_buku WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId",
            new { ids.BrgId, ids.DoId });
        return (stok, buku);
    }

    private static int CountLedgerMovements(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokMovement
            WHERE SourceTransactionId = @SourceTxId
            """,
            new { SourceTxId = ids.SourceTxId });
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

    private static void Cleanup(CaseIds ids)
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

    private sealed record CaseIds(
        string SourceTxId,
        string BrgId,
        string DoId,
        string PoId,
        string LocationId,
        string LocationId2,
        IStockLedgerScopeKey Scope);

    private sealed record Repos(
        IStockMovementRepo Movement,
        IStockPositionRepo Position,
        IStockLedgerScopeStateRepo Scope,
        IStockSourceIdempotencyRepo Idempotency);

    private sealed record Harness(
        PostDoReceiptStockConsequenceHandler Handler,
        LegacyStockReadPort LegacyRead,
        Repos Repos);
}
