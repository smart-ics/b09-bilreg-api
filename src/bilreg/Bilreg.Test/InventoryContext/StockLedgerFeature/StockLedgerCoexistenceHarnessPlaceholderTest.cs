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
/// G-23 coexistence harness scaffolding.
/// Phase 2 activates only the reconstruction-relevant depleted-layer intentional difference.
/// Phase 3/4/5/8 scenarios remain skipped placeholders.
/// </summary>
public class StockLedgerCoexistenceHarnessPlaceholderTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T3 = new(2024, 2, 1, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);
    private static readonly DateOnly ExpB = new(2025, 6, 1);

    [Fact(Skip = "G-23 scaffolding — Legacy→New synchronization scenario requires Phase 3 discovery + sync.")]
    public void LegacyToNew_SynchronizesLegacyOriginatedChange()
    {
        Assert.Fail("Not implemented: Legacy→New coexistence scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — New→Legacy visibility requires Phase 4 live Legacy Compatibility Writer.")]
    public void NewToLegacy_ReceiptVisibleInLegacyAuthority()
    {
        Assert.Fail("Not implemented: New→Legacy coexistence scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — alternating writers requires Phase 3/4 mixed-writer enablement.")]
    public void AlternatingWriters_RemainReconcileable()
    {
        Assert.Fail("Not implemented: alternating writers scenario.");
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
            // Surviving legacy balances omit LY02 (fully consumed / deleted zero row).
            // Journals still prove the depleted provenance at LY02.
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

            var (sut, _, _, repos) = CreateSut(legacyBalances, journals);
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

            // Representational: Ledger retains depleted layer; legacy balance list still has no LY02.
            legacyBalances.Should().HaveCount(1);
            CountLayers(key).Should().Be(2);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact(Skip = "G-23 scaffolding — real mismatch classification requires Phase 3/8 reconciliation behavior.")]
    public void RealMismatch_IsClassifiedAndSurfaced()
    {
        Assert.Fail("Not implemented: real mismatch scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — duplicate sync batch covered when Phase 3 sync UoW exists.")]
    public void DuplicateSyncBatch_IsIdempotent()
    {
        Assert.Fail("Not implemented: duplicate sync batch scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — concurrent outbound requires Phase 5 capability + OCC hardening.")]
    public void ConcurrentOutbound_RespectsWriteConsistencyScope()
    {
        Assert.Fail("Not implemented: concurrent outbound scenario.");
    }

    [Fact(Skip = "G-23 scaffolding — partial-failure rollback across live legacy+Ledger is Phase 4+ (Ledger-only proven in P1-S8).")]
    public void PartialFailure_LegacyAndLedger_RollBackTogether()
    {
        Assert.Fail("Not implemented: live legacy+Ledger partial-failure scenario.");
    }

    private static (
        ReconstructStockLedgerBaselineHandler Sut,
        SpyUnitOfWork Spy,
        FakeLegacyCompatibilityWriterPort Legacy,
        Repos Repos)
        CreateSut(
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
        var movementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key);
        var idempotencyKey = $"RECON|{key.BrgId}|{key.ReceiptSourceId}|baseline";
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

    private sealed record Repos(
        IStockLedgerScopeStateRepo Scope,
        IStockMovementRepo Movement,
        IStockPositionRepo Position,
        IStockSourceIdempotencyRepo Idempotency);
}
