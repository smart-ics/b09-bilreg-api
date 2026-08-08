using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
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
/// P4-S4 — Native DO Receipt void consequence against disposable <c>devTest</c>.
/// Never writes <c>HOSPITAL_HPL</c>.
/// </summary>
public class VoidDoReceiptStockConsequenceHandlerTest
{
    private static readonly DateTime BusinessTime = new(2026, 8, 8, 11, 0, 0);
    private static readonly DateTime VoidTime = new(2026, 8, 8, 12, 0, 0);
    private static readonly DateTime ProcessedAt = new(2026, 8, 8, 12, 5, 0);

    [Fact]
    public async Task HappyPath_PostThenVoid_RetainsHistory_AndClearsLegacyBalance()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("HP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var post = await harness.Post.Handle(BuildPostCommand(ids), default);
            post.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);
            var priorPosition = post.SynchronizationPosition!;

            var voidResult = await harness.Void.Handle(BuildVoidCommand(ids), default);

            voidResult.Outcome.Should().Be(VoidDoReceiptStockConsequenceOutcomeEnum.Committed);
            voidResult.StockMovementId.Should().NotBeNullOrWhiteSpace();
            voidResult.OriginalStockMovementId.Should().Be(post.StockMovementId);
            voidResult.ScopeState!.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
            voidResult.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
            voidResult.SynchronizationPosition!.AlgorithmVersion.Should().Be(
                LegacyReconstructionBasisCalculator.AlgorithmVersion);
            voidResult.SynchronizationPosition.Should().NotBe(priorPosition);

            var original = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(post.StockMovementId!));
            original.HasValue.Should().BeTrue();
            original.Value.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
            original.Value.Origin.Should().Be(StockFactOriginEnum.Native);

            var reversal = harness.Repos.Movement
                .LoadEntity(StockMovementModel.Key(voidResult.StockMovementId!));
            reversal.HasValue.Should().BeTrue();
            reversal.Value.MovementKind.Should().Be(StockMovementKindEnum.Reversal);
            reversal.Value.ReversedMovementId.Should().Be(post.StockMovementId);
            reversal.Value.Origin.Should().Be(StockFactOriginEnum.Native);

            var position = harness.Repos.Position.LoadEntity(
                StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.LocationId));
            position.HasValue.Should().BeTrue();
            position.Value.Layers.Should().ContainSingle();
            position.Value.Layers[0].RemainingQuantity.Should().Be(0m);
            position.Value.Layers[0].InitialQuantity.Should().Be(10m);

            harness.LegacyRead.ListCurrentBalances(ids.Scope).Should().BeEmpty();
            var journals = harness.LegacyRead.ListJournalEntries(ids.Scope);
            journals.Should().HaveCount(2);
            journals.Should().Contain(j => j.MutationKindId == "DO" && j.QuantityIn == 10m);
            journals.Should().Contain(j => j.MutationKindId == "DO_V" && j.QuantityOut == 10m);

            var expected = LegacyReconstructionBasisCalculator.Compute(
                harness.LegacyRead.ListCurrentBalances(ids.Scope),
                journals);
            voidResult.SynchronizationPosition.Should().Be(expected);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task DuplicateVoid_AlreadyCommitted_QuantityNeutral()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("DUP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var post = await harness.Post.Handle(BuildPostCommand(ids), default);
            post.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);

            var first = await harness.Void.Handle(BuildVoidCommand(ids), default);
            first.Outcome.Should().Be(VoidDoReceiptStockConsequenceOutcomeEnum.Committed);

            var second = await harness.Void.Handle(BuildVoidCommand(ids), default);
            second.Outcome.Should().Be(VoidDoReceiptStockConsequenceOutcomeEnum.AlreadyCommitted);
            second.StockMovementId.Should().Be(first.StockMovementId);

            CountLedgerMovements(ids).Should().Be(2);
            CountLegacyRows(ids).Should().Be((0, 2));
            harness.LegacyRead.ListCurrentBalances(ids.Scope).Should().BeEmpty();
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task CapabilityDisabled_BlocksVoid_AfterPost()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("OFF");
        Cleanup(ids);

        try
        {
            var enabled = CreateLiveHarness(enabled: true);
            var post = await enabled.Post.Handle(BuildPostCommand(ids), default);
            post.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);

            var disabled = CreateLiveHarness(enabled: false);
            var voidResult = await disabled.Void.Handle(BuildVoidCommand(ids), default);

            voidResult.Outcome.Should().Be(VoidDoReceiptStockConsequenceOutcomeEnum.Disabled);
            voidResult.StockMovementId.Should().BeNull();

            CountLedgerMovements(ids).Should().Be(1);
            CountLegacyRows(ids).Should().Be((1, 1));
            enabled.LegacyRead.ListCurrentBalances(ids.Scope).Should().ContainSingle()
                .Which.Quantity.Should().Be(10m);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task InsufficientStock_AfterPartialConsume_FailsClosed()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("INS");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var post = await harness.Post.Handle(BuildPostCommand(ids), default);
            post.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);

            var writeScope = StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.LocationId);
            var position = harness.Repos.Position.LoadEntity(writeScope).Value;
            var layer = position.Layers.Single();
            var depleted = StockPositionModel.Create(
                position,
                [layer.Consume(3m)],
                position.Version + 1);
            harness.Repos.Position.SaveChanges(depleted);

            var voidResult = await harness.Void.Handle(BuildVoidCommand(ids), default);

            voidResult.Outcome.Should().Be(VoidDoReceiptStockConsequenceOutcomeEnum.InsufficientStock);
            voidResult.StockMovementId.Should().BeNull();
            CountLedgerMovements(ids).Should().Be(1);
            CountLegacyRows(ids).Should().Be((1, 1));
            harness.LegacyRead.ListJournalEntries(ids.Scope)
                .Should().NotContain(j => j.MutationKindId == "DO_V");
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task MultiLine_PostThenVoid_DepletesAllLines()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("ML");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var post = await harness.Post.Handle(BuildPostCommand(ids, multiLine: true), default);
            post.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);

            var voidResult = await harness.Void.Handle(BuildVoidCommand(ids), default);
            voidResult.Outcome.Should().Be(VoidDoReceiptStockConsequenceOutcomeEnum.Committed);

            var positions = harness.Repos.Position.ListByLedgerScope(ids.Scope);
            positions.Should().HaveCount(2);
            positions.Should().OnlyContain(p => p.TotalRemainingQuantity == 0m);

            harness.LegacyRead.ListCurrentBalances(ids.Scope).Should().BeEmpty();
            var journals = harness.LegacyRead.ListJournalEntries(ids.Scope);
            journals.Count(j => j.MutationKindId == "DO").Should().Be(2);
            journals.Count(j => j.MutationKindId == "DO_V").Should().Be(2);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public async Task ScopeFingerprint_Advances_WithFingerprintV1()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("FP");
        Cleanup(ids);

        try
        {
            var harness = CreateLiveHarness(enabled: true);
            var post = await harness.Post.Handle(BuildPostCommand(ids), default);
            post.Outcome.Should().Be(PostDoReceiptStockConsequenceOutcomeEnum.Committed);
            var prior = post.SynchronizationPosition!;

            var voidResult = await harness.Void.Handle(BuildVoidCommand(ids), default);
            voidResult.Outcome.Should().Be(VoidDoReceiptStockConsequenceOutcomeEnum.Committed);

            voidResult.SynchronizationPosition.Should().NotBe(prior);
            voidResult.SynchronizationPosition!.AlgorithmVersion.Should().Be("fingerprint-v1");

            var scope = harness.Repos.Scope.LoadEntity(ids.Scope).Value;
            scope.SynchronizationPosition.Should().Be(voidResult.SynchronizationPosition);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    private static PostDoReceiptStockConsequenceCommand BuildPostCommand(
        CaseIds ids,
        bool multiLine = false)
    {
        var lines = new List<DoReceiptLineFact>
        {
            new(
                LineNumber: 1,
                LayananId: ids.LocationId,
                Quantity: 10m,
                UnitCost: 1500.50m,
                ExpirationDate: new DateOnly(2027, 6, 30),
                Batch: "P4S4-BATCH",
                PurchaseOrderId: ids.PoId,
                SmallestUnitId: "TAB")
        };

        if (multiLine)
        {
            lines.Add(new DoReceiptLineFact(
                LineNumber: 2,
                LayananId: ids.LocationId2,
                Quantity: 5m,
                UnitCost: 1500.50m,
                ExpirationDate: new DateOnly(2027, 7, 31),
                Batch: "P4S4-BATCH-2",
                PurchaseOrderId: ids.PoId,
                SmallestUnitId: "TAB"));
        }

        return new PostDoReceiptStockConsequenceCommand(
            ids.BrgId,
            ids.DoId,
            ids.SourceTxId,
            BusinessTime,
            lines,
            ProcessedAt.AddHours(-1));
    }

    private static VoidDoReceiptStockConsequenceCommand BuildVoidCommand(CaseIds ids)
        => new(
            ids.BrgId,
            ids.DoId,
            OriginalSourceTransactionId: ids.SourceTxId,
            VoidSourceTransactionId: ids.VoidSourceTxId,
            EffectiveBusinessTime: VoidTime,
            ProcessedAt: ProcessedAt);

    private static Harness CreateLiveHarness(bool enabled)
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

        var options = Options.Create(new StockLedgerDoReceiptOptions { Enabled = enabled });
        var repos = new Repos(movementRepo, positionRepo, scopeRepo, idempotencyRepo);

        var post = new PostDoReceiptStockConsequenceHandler(
            options,
            scopeRepo,
            idempotencyRepo,
            positionRepo,
            consequenceUow,
            gate,
            legacyRead,
            bootstrapper);

        var voidHandler = new VoidDoReceiptStockConsequenceHandler(
            options,
            scopeRepo,
            idempotencyRepo,
            movementRepo,
            positionRepo,
            consequenceUow,
            gate,
            legacyRead);

        return new Harness(post, voidHandler, legacyRead, repos);
    }

    private static CaseIds CreateCaseIds(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 2 ? tag : tag[..2];
        var brgId = $"BRG{shortTag}{ulid}"[..13];
        var doId = $"DO{shortTag}{ulid}"[..10];
        return new CaseIds(
            SourceTxId: $"P4S4-{tag}-{ulid}",
            VoidSourceTxId: $"VOID-{tag}-{ulid}",
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
            WHERE SourceTransactionId IN (@SourceTxId, @VoidSourceTxId)
            """,
            new { ids.SourceTxId, ids.VoidSourceTxId });
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
                WHERE SourceTransactionId IN (@SourceTxId, @VoidSourceTxId));
            DELETE FROM BILRG_StokMovement
            WHERE StockMovementId IN (
                SELECT StockMovementId FROM BILRG_StokSourceIdempotency
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId AND StockMovementId <> ''
                UNION
                SELECT LayerFormingMovementId FROM BILRG_StokLayer
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
                UNION
                SELECT StockMovementId FROM BILRG_StokMovement
                WHERE SourceTransactionId IN (@SourceTxId, @VoidSourceTxId));
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
                ids.SourceTxId,
                ids.VoidSourceTxId
            });
    }

    private sealed record CaseIds(
        string SourceTxId,
        string VoidSourceTxId,
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
        PostDoReceiptStockConsequenceHandler Post,
        VoidDoReceiptStockConsequenceHandler Void,
        LegacyStockReadPort LegacyRead,
        Repos Repos);
}
