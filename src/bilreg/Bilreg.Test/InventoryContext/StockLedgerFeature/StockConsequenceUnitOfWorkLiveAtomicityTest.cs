using System.Data.SqlClient;
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

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S3 / G-18 — Live legacy + Ledger consequence atomicity with failure injection.
/// Uses disposable <c>devTest</c> only; never writes <c>HOSPITAL_HPL</c>.
/// </summary>
[Collection("StockLedgerP3S4")]
public class StockConsequenceUnitOfWorkLiveAtomicityTest
{
    private static readonly DateTime BusinessTime = new(2026, 8, 8, 14, 0, 0);
    private static readonly DateTime ProcessedAt = new(2026, 8, 8, 14, 5, 0);

    [Fact]
    public void Commit_LegacyApplyThrows_RollsBackLedgerAndLegacy()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("LEG");
        Cleanup(ids);

        try
        {
            var live = new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv());
            var legacy = new ThrowingLegacyCompatibilityWriterPort(live);
            var draft = BuildReceiptDraft(ids, lineCount: 1);
            var sut = CreateSut(legacy: legacy);

            var act = () => sut.Commit(draft);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Forced legacy Apply failure*");

            AssertZeroResidue(ids);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Commit_MovementSaveThrows_RollsBackIncludingIdempotency()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("MOV");
        Cleanup(ids);

        try
        {
            var repos = CreateRealRepos();
            var sut = CreateSut(
                movementRepo: new ThrowingStockMovementRepo(repos.Movement),
                positionRepo: repos.Position,
                scopeRepo: repos.Scope,
                idempotencyRepo: repos.Idempotency,
                legacy: new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv()));

            var act = () => sut.Commit(BuildReceiptDraft(ids, lineCount: 1));
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Forced movement persist failure*");

            AssertZeroResidue(ids);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Commit_PositionSaveThrows_RollsBackAll()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("POS");
        Cleanup(ids);

        try
        {
            var repos = CreateRealRepos();
            var sut = CreateSut(
                movementRepo: repos.Movement,
                positionRepo: new ThrowingStockPositionRepo(repos.Position),
                scopeRepo: repos.Scope,
                idempotencyRepo: repos.Idempotency,
                legacy: new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv()));

            var act = () => sut.Commit(BuildReceiptDraft(ids, lineCount: 1));
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Forced position persist failure*");

            AssertZeroResidue(ids);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Commit_ScopeSaveThrows_RollsBackAll()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("SCP");
        Cleanup(ids);

        try
        {
            var repos = CreateRealRepos();
            var sut = CreateSut(
                movementRepo: repos.Movement,
                positionRepo: repos.Position,
                scopeRepo: new ThrowingStockLedgerScopeStateRepo(repos.Scope),
                idempotencyRepo: repos.Idempotency,
                legacy: new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv()));

            var act = () => sut.Commit(BuildReceiptDraft(ids, lineCount: 1));
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Forced scope persist failure*");

            AssertZeroResidue(ids);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Commit_MidLegacyMultiRowWrite_RollsBackAll()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("MID");
        Cleanup(ids);

        try
        {
            var live = new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv());
            var partial = new PartialLegacyCompatibilityWriterPort(live, succeedLineCount: 1);
            var draft = BuildReceiptDraft(ids, lineCount: 2);
            var sut = CreateSut(legacy: partial);

            var act = () => sut.Commit(draft);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Forced mid-legacy multi-row failure*");

            partial.AppliedLineCount.Should().Be(1);
            AssertZeroResidue(ids);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Commit_HappyPathLiveWriter_ThenRetry_IsAlreadyCommitted()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("HAP");
        Cleanup(ids);

        try
        {
            var sut = CreateSut(legacy: new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv()));
            var firstDraft = BuildReceiptDraft(ids, lineCount: 1);

            var first = sut.Commit(firstDraft);
            first.Outcome.Should().Be(StockConsequenceCommitOutcomeEnum.Committed);

            CountLegacyRows(ids).Should().Be((1, 1));
            CountLedgerMovements(ids).Should().Be(1);
            CountLayers(ids).Should().Be(1);
            CountScopes(ids).Should().Be(1);
            CountIdempotency(ids).Should().Be(1);

            var retryMovementId = Ulid.NewUlid().ToString();
            var retryDraft = BuildReceiptDraft(
                ids with { MovementId = retryMovementId, LayerId = Ulid.NewUlid().ToString() },
                lineCount: 1,
                idempotencyKey: ids.IdempotencyKey);

            var retry = sut.Commit(retryDraft);
            retry.Outcome.Should().Be(StockConsequenceCommitOutcomeEnum.AlreadyCommitted);
            retry.StockMovementId.Should().Be(ids.MovementId);

            CountLegacyRows(ids).Should().Be((1, 1));
            CountLedgerMovements(ids).Should().Be(1);
            CountLayers(ids).Should().Be(1);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    private static StockConsequenceUnitOfWork CreateSut(
        ILegacyCompatibilityWriterPort? legacy = null,
        IStockMovementRepo? movementRepo = null,
        IStockPositionRepo? positionRepo = null,
        IStockLedgerScopeStateRepo? scopeRepo = null,
        IStockSourceIdempotencyRepo? idempotencyRepo = null)
    {
        var repos = CreateRealRepos();
        return new StockConsequenceUnitOfWork(
            new TransHelperUnitOfWork(),
            idempotencyRepo ?? repos.Idempotency,
            movementRepo ?? repos.Movement,
            positionRepo ?? repos.Position,
            scopeRepo ?? repos.Scope,
            legacy ?? new LegacyCompatibilityWriterPort(ConnStringHelper.GetTestEnv()),
            repos.Binding);
    }

    private static Repos CreateRealRepos()
    {
        var options = ConnStringHelper.GetTestEnv();
        return new Repos(
            new StockMovementRepo(
                new StockMovementDal(options),
                new StockMovementLineDal(options)),
            new StockPositionRepo(
                new StockPositionDal(options),
                new StockLayerDal(options)),
            new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options)),
            new StockSourceIdempotencyRepo(new StockSourceIdempotencyDal(options)),
            new StockLayerLegacyBindingRepo(new StockLayerLegacyBindingDal(options)));
    }

    internal static StockConsequenceDraft BuildReceiptDraft(
        CaseIds ids,
        int lineCount,
        string? idempotencyKey = null)
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
                Batch: "P4S3-BATCH",
                PurchaseOrderId: ids.PoId,
                SmallestUnitId: "TAB"));
        }

        var item = new BrgReff(ids.BrgId, ids.BrgId);
        var receiptSource = ReceiptSourceType.Key(ids.DoId);
        var movementKey = StockMovementModel.Key(ids.MovementId);

        var movementLines = new List<StockMovementLineType>(lineCount);
        var layersByLocation = new Dictionary<string, List<StockLayerModel>>(StringComparer.Ordinal);

        // First line uses the case LayerId for stable cleanup assertions; extras get new ids.
        for (var i = 0; i < lines.Count; i++)
        {
            var lineFact = lines[i];
            var location = LayananType.Key(lineFact.LayananId);
            var layerId = i == 0 ? ids.LayerId : Ulid.NewUlid().ToString();
            var layer = StockLayerModel.Create(
                item,
                receiptSource,
                location,
                movementKey,
                initialQuantity: lineFact.Quantity,
                UnitValuationType.Create(lineFact.UnitCost),
                BusinessTime,
                StockFactOriginEnum.Native,
                expirationDate: lineFact.ExpirationDate,
                batch: lineFact.Batch,
                stockLayerId: layerId);

            movementLines.Add(StockMovementLineType.Create(
                lineFact.LineNumber,
                item,
                receiptSource,
                location,
                StockMovementDirectionEnum.Inbound,
                lineFact.Quantity,
                UnitValuationType.Create(lineFact.UnitCost),
                StockFactOriginEnum.Native,
                layer));

            if (!layersByLocation.TryGetValue(location.LayananId, out var list))
            {
                list = [];
                layersByLocation[location.LayananId] = list;
            }

            list.Add(layer);
        }

        var movement = StockMovementModel.CreateReceipt(
            SourceTransactionReferenceType.Key(ids.SourceTxId),
            BusinessTime,
            movementLines,
            StockFactOriginEnum.Native,
            stockMovementId: ids.MovementId);

        var positions = layersByLocation
            .Select(kvp =>
            {
                var position = StockPositionModel.CreateEmpty(
                    item,
                    receiptSource,
                    LayananType.Key(kvp.Key));
                foreach (var layer in kvp.Value)
                    position = position.AddLayer(layer);
                return position;
            })
            .ToList();

        var legacyWrite = DoReceiptLegacyCompatibilityMapper.Map(
            SourceTransactionReferenceType.Key(ids.SourceTxId),
            ids.Scope,
            BusinessTime,
            lines);

        var snapshot = DoReceiptLegacyCompatibilityMapper.ToFingerprintSnapshot(legacyWrite);
        var fingerprint = LegacyReconstructionBasisCalculator.Compute(
            snapshot.Balances,
            snapshot.Journals);

        var scope = StockLedgerScopeStateModel
            .CreateNotReconstructed(ids.Scope)
            .EstablishFromNativeReceipt(
                fingerprint,
                LegacyReconstructionBasisCalculator.AlgorithmVersion);

        return new StockConsequenceDraft(
            idempotencyKey ?? ids.IdempotencyKey,
            ProcessedAt,
            movement,
            positions,
            scope,
            legacyWrite,
            StockSourceIdempotencyKindEnum.SourceConsequence);
    }

    internal static CaseIds CreateCaseIds(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 3 ? tag : tag[..3];
        var brgId = $"BRG{shortTag}{ulid}"[..13];
        var doId = $"DO{shortTag}{ulid}"[..10];
        return new CaseIds(
            IdempotencyKey: DoReceiptConsequenceIdempotency.BuildSourceConsequenceKey(
                StockLedgerScopeKeyType.Create(brgId, doId),
                $"P4S3-{tag}-{ulid}"),
            SourceTxId: $"P4S3-{tag}-{ulid}",
            MovementId: ulid,
            LayerId: Ulid.NewUlid().ToString(),
            BrgId: brgId,
            DoId: doId,
            PoId: $"PO{shortTag}{ulid}"[..10],
            LocationId: $"G{shortTag}{ulid}"[..5],
            LocationId2: $"H{shortTag}{ulid}"[..5],
            Scope: StockLedgerScopeKeyType.Create(brgId, doId));
    }

    internal static void AssertZeroResidue(CaseIds ids)
    {
        CountLedgerMovements(ids).Should().Be(0, "no durable movement after rollback");
        CountLayers(ids).Should().Be(0, "no durable layers after rollback");
        CountPositions(ids).Should().Be(0, "no durable positions after rollback");
        CountScopes(ids).Should().Be(0, "no durable scope after rollback");
        CountIdempotency(ids).Should().Be(0, "no durable SourceConsequence idempotency after rollback");
        CountLegacyRows(ids).Should().Be((0, 0), "no durable tb_stok/tb_buku after rollback");
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
            WHERE SourceTransactionId = @SourceTxId OR StockMovementId = @MovementId
            """,
            new { SourceTxId = ids.SourceTxId, MovementId = ids.MovementId });
    }

    private static int CountLayers(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokLayer
            WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
            """,
            new { ids.BrgId, ids.DoId });
    }

    private static int CountPositions(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokPosition
            WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
            """,
            new { ids.BrgId, ids.DoId });
    }

    private static int CountScopes(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokLedgerScope
            WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
            """,
            new { ids.BrgId, ids.DoId });
    }

    private static int CountIdempotency(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokSourceIdempotency
            WHERE IdempotencyKey = @IdempotencyKey
               OR (BrgId = @BrgId AND ReceiptSourceId = @DoId)
            """,
            new { ids.IdempotencyKey, ids.BrgId, ids.DoId });
    }

    internal static void Cleanup(CaseIds ids)
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
                WHERE SourceTransactionId = @SourceTxId
                UNION
                SELECT @MovementId);
            DELETE FROM BILRG_StokMovement
            WHERE StockMovementId IN (
                SELECT StockMovementId FROM BILRG_StokSourceIdempotency
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId AND StockMovementId <> ''
                UNION
                SELECT LayerFormingMovementId FROM BILRG_StokLayer
                WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
                UNION
                SELECT StockMovementId FROM BILRG_StokMovement
                WHERE SourceTransactionId = @SourceTxId
                UNION
                SELECT @MovementId);
            DELETE FROM BILRG_StokLayerLegacyBinding WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokLayer WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokPosition WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokLedgerScope WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokSourceIdempotency
            WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId
               OR IdempotencyKey = @IdempotencyKey;
            DELETE FROM tb_stok WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId;
            DELETE FROM tb_buku WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId;
            """,
            new
            {
                ids.BrgId,
                ids.DoId,
                ids.SourceTxId,
                ids.MovementId,
                ids.IdempotencyKey
            });
    }

    internal sealed record CaseIds(
        string IdempotencyKey,
        string SourceTxId,
        string MovementId,
        string LayerId,
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
        IStockSourceIdempotencyRepo Idempotency,
        IStockLayerLegacyBindingRepo Binding);
}
