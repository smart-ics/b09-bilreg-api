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
/// P3-S3 / G-16 P0 — Live material reconciliation adapter on disposable fixtures.
/// Classify only; no catch-up, repair, or Synchronization Position advancement.
/// </summary>
public class StockReconciliationPortTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T3 = new(2024, 2, 1, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);
    private static readonly DateOnly ExpB = new(2025, 6, 1);

    [Fact]
    public async Task DepletedLayer_AfterReconstruction_IsIntentionalDifferenceOrBalanced()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("DEP");
        Cleanup(key);

        try
        {
            var snapshot = DepletedLayerSnapshot(key);
            var (handler, fakeRead, repos, layerDal) = CreateStack(snapshot);
            var recon = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            recon.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);

            var sut = new StockReconciliationPort(fakeRead, layerDal, repos.Scope);
            var result = sut.Reconcile(key);

            result.Outcome.Should().BeOneOf(
                StockReconciliationOutcomeEnum.Balanced,
                StockReconciliationOutcomeEnum.IntentionalDifference);
            result.Outcome.Should().NotBe(StockReconciliationOutcomeEnum.MaterialInconsistency);
            result.LegacyRemainingQuantity.Should().Be(5m);
            result.LedgerRemainingQuantity.Should().Be(5m);
            StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeTrue();

            if (result.Outcome == StockReconciliationOutcomeEnum.IntentionalDifference)
            {
                result.Differences.Should().Contain(d =>
                    d.Kind == StockReconciliationDifferenceKindEnum.DepletedLayerVsAbsentLegacyRow
                    && d.LayananId == "LY02");
            }
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task QuantityMismatch_AfterLayerTamper_IsMaterialInconsistency()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("MIS");
        Cleanup(key);

        try
        {
            var snapshot = DepletedLayerSnapshot(key);
            var (handler, fakeRead, repos, layerDal) = CreateStack(snapshot);
            var recon = await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);
            recon.Outcome.Should().Be(ReconstructStockLedgerBaselineOutcomeEnum.Reconstructed);

            TamperLayerRemainingQuantity(key, "LY01", newRemaining: 1m);

            var sut = new StockReconciliationPort(fakeRead, layerDal, repos.Scope);
            var result = sut.Reconcile(key);

            result.Outcome.Should().Be(StockReconciliationOutcomeEnum.MaterialInconsistency);
            result.Differences.Should().Contain(d =>
                d.Kind == StockReconciliationDifferenceKindEnum.QuantityMismatch
                && d.LayananId == "LY01");
            StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeFalse();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task Reconcile_IsReadOnly_DoesNotMutateLedgerOrScope()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("RO");
        Cleanup(key);

        try
        {
            var snapshot = DepletedLayerSnapshot(key);
            var (handler, fakeRead, repos, layerDal) = CreateStack(snapshot);
            await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            var layersBefore = CountLayers(key);
            var positionsBefore = CountPositions(key);
            var scopeBefore = repos.Scope.LoadEntity(key).Value;
            var movementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key);
            var movementBefore = repos.Movement.LoadEntity(StockMovementModel.Key(movementId)).HasValue;

            var sut = new StockReconciliationPort(fakeRead, layerDal, repos.Scope);
            _ = sut.Reconcile(key);
            _ = sut.Reconcile(key);

            CountLayers(key).Should().Be(layersBefore);
            CountPositions(key).Should().Be(positionsBefore);
            repos.Movement.LoadEntity(StockMovementModel.Key(movementId)).HasValue.Should().Be(movementBefore);

            var scopeAfter = repos.Scope.LoadEntity(key).Value;
            scopeAfter.ReconstructionStatus.Should().Be(scopeBefore.ReconstructionStatus);
            scopeAfter.SynchronizationState.Should().Be(scopeBefore.SynchronizationState);
            scopeAfter.SynchronizationPosition.Should().Be(scopeBefore.SynchronizationPosition);
            scopeAfter.InconsistencyReason.Should().Be(scopeBefore.InconsistencyReason);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task ScopeSynchronizationRequired_ReturnsPendingSynchronization_MaterialSafe()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("PEN");
        Cleanup(key);

        try
        {
            var snapshot = DepletedLayerSnapshot(key);
            var (handler, fakeRead, repos, layerDal) = CreateStack(snapshot);
            await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            var current = repos.Scope.LoadEntity(key).Value;
            repos.Scope.SaveChanges(current.RequireSynchronization());

            var sut = new StockReconciliationPort(fakeRead, layerDal, repos.Scope);
            var result = sut.Reconcile(key);

            result.Outcome.Should().Be(StockReconciliationOutcomeEnum.PendingSynchronization);
            result.LegacyRemainingQuantity.Should().Be(5m);
            result.LedgerRemainingQuantity.Should().Be(5m);
            StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeTrue();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task ScopeLegacyChangePending_ReturnsPendingSynchronization_MaterialSafe()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("LCP");
        Cleanup(key);

        try
        {
            var snapshot = DepletedLayerSnapshot(key);
            var (handler, fakeRead, repos, layerDal) = CreateStack(snapshot);
            await handler.Handle(
                new ReconstructStockLedgerBaselineCommand(key.BrgId, key.ReceiptSourceId),
                default);

            var current = repos.Scope.LoadEntity(key).Value;
            repos.Scope.SaveChanges(current.MarkLegacyChangePending());

            var sut = new StockReconciliationPort(fakeRead, layerDal, repos.Scope);
            var result = sut.Reconcile(key);

            result.Outcome.Should().Be(StockReconciliationOutcomeEnum.PendingSynchronization);
            result.LegacyRemainingQuantity.Should().Be(5m);
            result.LedgerRemainingQuantity.Should().Be(5m);
            StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeTrue();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void UnreconstructedScope_ReturnsProvenanceLimitation()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("PRV");
        Cleanup(key);

        try
        {
            var options = ConnStringHelper.GetTestEnv();
            var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
            var layerDal = new StockLayerDal(options);
            var fakeRead = new FakeLegacyStockReadPort
            {
                Balances = [],
                JournalEntries = []
            };

            scopeRepo.SaveChanges(StockLedgerScopeStateModel.CreateNotReconstructed(key));

            var sut = new StockReconciliationPort(fakeRead, layerDal, scopeRepo);
            var result = sut.Reconcile(key);

            result.Outcome.Should().Be(StockReconciliationOutcomeEnum.ProvenanceLimitation);
            result.LegacyRemainingQuantity.Should().BeNull();
            result.LedgerRemainingQuantity.Should().BeNull();
            StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeFalse();
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void MissingScope_ReturnsProvenanceLimitation()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("ABS");
        Cleanup(key);

        try
        {
            var options = ConnStringHelper.GetTestEnv();
            var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
            var layerDal = new StockLayerDal(options);
            var fakeRead = new FakeLegacyStockReadPort();

            var sut = new StockReconciliationPort(fakeRead, layerDal, scopeRepo);
            var result = sut.Reconcile(key);

            result.Outcome.Should().Be(StockReconciliationOutcomeEnum.ProvenanceLimitation);
            StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeFalse();
        }
        finally
        {
            Cleanup(key);
        }
    }

    private static (
        ReconstructStockLedgerBaselineHandler Handler,
        FakeLegacyStockReadPort FakeRead,
        Repos Repos,
        IStockLayerDal LayerDal)
        CreateStack(Snapshot snapshot)
    {
        var options = ConnStringHelper.GetTestEnv();
        var spy = new SpyUnitOfWork();
        var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
        var movementRepo = new StockMovementRepo(
            new StockMovementDal(options),
            new StockMovementLineDal(options));
        var layerDal = new StockLayerDal(options);
        var positionRepo = new StockPositionRepo(new StockPositionDal(options), layerDal);
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
        var handler = new ReconstructStockLedgerBaselineHandler(
            claim,
            fakeRead,
            uow,
            spy,
            scopeRepo,
            idempotencyRepo);

        return (handler, fakeRead, new Repos(scopeRepo, movementRepo, positionRepo, idempotencyRepo), layerDal);
    }

    private static Snapshot DepletedLayerSnapshot(IStockLedgerScopeKey key)
        => new(
            [
                Balance(key, "LY01", 5m, 1000m, ExpA, "STK001", T1, "B1")
            ],
            [
                Journal(key, "TRS001", "LY01", qtyIn: 5m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
                Journal(key, "TRS002", "LY02", qtyIn: 8m, qtyOut: 0m, 1200m, ExpB, T2, "B2"),
                Journal(key, "TRS003", "LY02", qtyIn: 0m, qtyOut: 8m, 1200m, ExpB, T3, "B2")
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
            $"BRGR03{shortTag}{ulid}"[..13],
            $"DOR03{shortTag}{ulid}"[..10]);
    }

    private static void TamperLayerRemainingQuantity(
        IStockLedgerScopeKey key,
        string layananId,
        decimal newRemaining)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var updated = conn.Execute(
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
            });
        updated.Should().BeGreaterThan(0);
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

    private sealed record Snapshot(
        IReadOnlyList<LegacyStockBalanceType> Balances,
        IReadOnlyList<LegacyStockJournalEntryType> Journals);

    private sealed record Repos(
        IStockLedgerScopeStateRepo Scope,
        IStockMovementRepo Movement,
        IStockPositionRepo Position,
        IStockSourceIdempotencyRepo Idempotency);
}
