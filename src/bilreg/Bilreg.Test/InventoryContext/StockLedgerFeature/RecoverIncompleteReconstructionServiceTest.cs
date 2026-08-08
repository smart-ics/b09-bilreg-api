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
/// P2-S8 — Controlled recovery of incomplete additive reconstruction output.
/// Disposable <c>devTest</c> only; never modifies <c>tb_stok</c> / <c>tb_buku</c>.
/// </summary>
public class RecoverIncompleteReconstructionServiceTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);
    private static readonly DateOnly ExpB = new(2025, 6, 1);

    [Fact]
    public void Recover_WhenNothingExists_IsNoOp()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("NOP");
        TestCleanup(key);

        try
        {
            var sut = CreateRecovery();
            var result = sut.Recover(key);

            result.Outcome.Should().Be(RecoverIncompleteReconstructionOutcomeEnum.NoOp);
            result.DeletedRowCount.Should().Be(0);
            ScopeExists(key).Should().BeFalse();
        }
        finally
        {
            TestCleanup(key);
        }
    }

    [Fact]
    public async Task Recover_StuckReconstructing_AllowsSuccessfulReconstruction()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var key = NewScopeKey("STK");
        TestCleanup(key);

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

            var stuckRead = new FakeLegacyStockReadPort
            {
                BalancesByCall = call => call % 2 == 1 ? phaseB.Balances : changed.Balances,
                JournalsByCall = call => call % 2 == 1 ? phaseB.Journals : changed.Journals
            };
            var (handler, _, _, repos) = CreateHandler(stuckRead);

            var stuck = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            stuck.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.BasisChangedRetryRequired);
            repos.Scope.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructing);

            var recovery = CreateRecovery();
            var recovered = recovery.Recover(key);
            recovered.Outcome.Should().Be(RecoverIncompleteReconstructionOutcomeEnum.Recovered);
            repos.Scope.LoadEntity(key).HasValue.Should().BeFalse();

            var (freshHandler, _, _, freshRepos) = CreateHandler(phaseB);
            var result = await freshHandler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            result.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);
            freshRepos.Scope.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructed);
            CountLayers(key).Should().BeGreaterThan(0);
        }
        finally
        {
            TestCleanup(key);
        }
    }

    [Fact]
    public async Task Recover_ClearsTerminalReconstructed_AllowingOverwritePath()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("TRM");
        TestCleanup(key);

        try
        {
            var snapshot = MultiLocationBalancedSnapshot(key);
            var (handler, _, _, repos) = CreateHandler(snapshot);

            var first = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            first.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);

            var blocked = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            blocked.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.AlreadyReconstructed);

            var recovery = CreateRecovery();
            recovery.Recover(key).Outcome.Should().Be(RecoverIncompleteReconstructionOutcomeEnum.Recovered);
            repos.Scope.LoadEntity(key).HasValue.Should().BeFalse();
            CountLayers(key).Should().Be(0);
            CountMovements(key).Should().Be(0);

            var second = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            second.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);
        }
        finally
        {
            TestCleanup(key);
        }
    }

    [Fact]
    public async Task Recover_ClearsTerminalInconsistent_AllowingRetry()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("INC");
        TestCleanup(key);

        try
        {
            var inconsistent = new Snapshot(
                [Balance(key, "LY01", 10m, 1000m, ExpA, "STK001", T1, "B1")],
                [
                    Journal(key, "TRS001", "LY01", qtyIn: 10m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
                    Journal(key, "TRS002", "LY01", qtyIn: 0m, qtyOut: 3m, 1000m, ExpA, T2, "B1")
                ]);

            var (handler, _, _, repos) = CreateHandler(inconsistent);
            var first = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            first.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Inconsistent);

            var blocked = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            blocked.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.AlreadyInconsistent);

            CreateRecovery().Recover(key).Outcome
                .Should().Be(RecoverIncompleteReconstructionOutcomeEnum.Recovered);
            repos.Scope.LoadEntity(key).HasValue.Should().BeFalse();

            // After recovery, same inconsistent facts still classify as Inconsistent (not blocked).
            var again = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            again.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Inconsistent);
        }
        finally
        {
            TestCleanup(key);
        }
    }

    [Fact]
    public async Task Recover_DoesNotModifyLegacyStockRows()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var key = NewScopeKey("LEG");
        TestCleanup(key);

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var stokBefore = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_stok");
        var bukuBefore = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_buku");

        try
        {
            var snapshot = MultiLocationBalancedSnapshot(key);
            var (handler, _, legacy, _) = CreateHandler(snapshot);
            await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            CreateRecovery().Recover(key).Outcome
                .Should().Be(RecoverIncompleteReconstructionOutcomeEnum.Recovered);

            conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_stok").Should().Be(stokBefore);
            conn.ExecuteScalar<int>("SELECT COUNT(1) FROM tb_buku").Should().Be(bukuBefore);
            legacy.Applied.Should().BeEmpty();
        }
        finally
        {
            TestCleanup(key);
        }
    }

    private static RecoverIncompleteReconstructionService CreateRecovery()
    {
        var options = ConnStringHelper.GetTestEnv();
        return new RecoverIncompleteReconstructionService(
            new TransHelperUnitOfWork(),
            new IncompleteReconstructionRecoveryPort(options));
    }

    private static (
        ReconstructStockLedgerBaselineHandler Sut,
        SpyUnitOfWork Spy,
        FakeLegacyCompatibilityWriterPort Legacy,
        Repos Repos)
        CreateHandler(Snapshot snapshot)
    {
        var fakeRead = new FakeLegacyStockReadPort
        {
            Balances = snapshot.Balances,
            JournalEntries = snapshot.Journals
        };
        return CreateHandler(fakeRead);
    }

    private static (
        ReconstructStockLedgerBaselineHandler Sut,
        SpyUnitOfWork Spy,
        FakeLegacyCompatibilityWriterPort Legacy,
        Repos Repos)
        CreateHandler(FakeLegacyStockReadPort fakeRead)
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
            $"BRGS8{shortTag}{ulid}"[..13],
            $"DOS8{shortTag}{ulid}"[..10]);
    }

    private static void TestCleanup(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var movementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key);
        var idempotencyKey = RecoverIncompleteReconstructionService.BuildReconstructionIdempotencyKey(key);
        conn.Execute(
            """
            DELETE FROM BILRG_StokMovementLine WHERE StockMovementId = @MovementId;
            DELETE FROM BILRG_StokMovement WHERE StockMovementId = @MovementId;
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

    private static bool ScopeExists(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokLedgerScope
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
            """,
            new { key.BrgId, key.ReceiptSourceId }) > 0;
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
