using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
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
/// P1-S8 — Stock Consequence UoW foundation: atomic Ledger commit/rollback with fake legacy writer.
/// Uses disposable <c>devTest</c> only; never writes <c>tb_stok</c> / <c>tb_buku</c>.
/// </summary>
public class StockConsequenceUnitOfWorkTest
{
    private static readonly DateTime BusinessTime = new(2026, 8, 7, 14, 0, 0);
    private static readonly DateTime ProcessedAt = new(2026, 8, 7, 14, 5, 0);

    private readonly IStockMovementRepo _movementRepo;
    private readonly IStockPositionRepo _positionRepo;
    private readonly IStockLedgerScopeStateRepo _scopeRepo;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;

    public StockConsequenceUnitOfWorkTest()
    {
        var options = ConnStringHelper.GetTestEnv();
        _movementRepo = new StockMovementRepo(
            new StockMovementDal(options),
            new StockMovementLineDal(options));
        _positionRepo = new StockPositionRepo(
            new StockPositionDal(options),
            new StockLayerDal(options));
        _scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
        _idempotencyRepo = new StockSourceIdempotencyRepo(new StockSourceIdempotencyDal(options));
    }

    [Fact]
    public void Commit_HappyPath_PersistsMovementPositionScopeIdempotency_AndCallsLegacyFake()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var ids = CreateCaseIds("HAPPY");
        Cleanup(ids);

        try
        {
            var legacy = new FakeLegacyCompatibilityWriterPort();
            var sut = CreateSut(legacy);
            var draft = BuildReceiptDraft(ids);

            var result = sut.Commit(draft);

            result.Outcome.Should().Be(StockConsequenceCommitOutcomeEnum.Committed);
            result.StockMovementId.Should().Be(ids.MovementId);
            result.IdempotencyKey.Should().Be(ids.IdempotencyKey);

            _movementRepo.LoadEntity(StockMovementModel.Key(ids.MovementId)).HasValue.Should().BeTrue();
            _positionRepo.LoadEntity(StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.LocationId))
                .HasValue.Should().BeTrue();
            _scopeRepo.LoadEntity(StockLedgerScopeKeyType.Create(ids.BrgId, ids.DoId))
                .HasValue.Should().BeTrue();
            _idempotencyRepo.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(
                        StockSourceIdempotencyKindEnum.SourceConsequence,
                        ids.IdempotencyKey))
                .HasValue.Should().BeTrue();

            legacy.Applied.Should().ContainSingle();
            legacy.Applied[0].SourceTransaction.SourceTransactionId.Should().Be(ids.SourceTxId);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Commit_WhenLegacyFakeThrows_RollsBackAllLedgerRows()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var ids = CreateCaseIds("ROLLBACK");
        Cleanup(ids);

        try
        {
            var legacy = new FakeLegacyCompatibilityWriterPort { ThrowOnApply = true };
            var sut = CreateSut(legacy);
            var draft = BuildReceiptDraft(ids);

            var act = () => sut.Commit(draft);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Fake legacy compatibility writer failure*");

            _movementRepo.LoadEntity(StockMovementModel.Key(ids.MovementId)).HasValue.Should().BeFalse();
            _positionRepo.LoadEntity(StockWriteScopeKeyType.Create(ids.BrgId, ids.DoId, ids.LocationId))
                .HasValue.Should().BeFalse();
            _scopeRepo.LoadEntity(StockLedgerScopeKeyType.Create(ids.BrgId, ids.DoId))
                .HasValue.Should().BeFalse();
            _idempotencyRepo.LoadByBusinessKey(
                    StockSourceIdempotencyModel.BusinessKey(
                        StockSourceIdempotencyKindEnum.SourceConsequence,
                        ids.IdempotencyKey))
                .HasValue.Should().BeFalse();
            legacy.Applied.Should().BeEmpty();
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Commit_DuplicateIdempotencyKey_ReturnsAlreadyCommitted_WithoutSecondWrite()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var ids = CreateCaseIds("IDEM");
        Cleanup(ids);

        try
        {
            var legacy = new FakeLegacyCompatibilityWriterPort();
            var sut = CreateSut(legacy);

            var first = sut.Commit(BuildReceiptDraft(ids));
            first.Outcome.Should().Be(StockConsequenceCommitOutcomeEnum.Committed);
            legacy.Applied.Should().HaveCount(1);

            // Second attempt uses a different movement id but the same source idempotency key.
            var retryMovementId = Ulid.NewUlid().ToString();
            var retryLayerId = Ulid.NewUlid().ToString();
            var retryIds = ids with
            {
                MovementId = retryMovementId,
                LayerId = retryLayerId
            };
            var retry = sut.Commit(BuildReceiptDraft(retryIds, idempotencyKey: ids.IdempotencyKey));

            retry.Outcome.Should().Be(StockConsequenceCommitOutcomeEnum.AlreadyCommitted);
            retry.StockMovementId.Should().Be(ids.MovementId);
            retry.IdempotencyId.Should().Be(first.IdempotencyId);
            legacy.Applied.Should().HaveCount(1);

            _movementRepo.LoadEntity(StockMovementModel.Key(ids.MovementId)).HasValue.Should().BeTrue();
            _movementRepo.LoadEntity(StockMovementModel.Key(retryMovementId)).HasValue.Should().BeFalse();
        }
        finally
        {
            Cleanup(ids);
        }
    }

    private StockConsequenceUnitOfWork CreateSut(ILegacyCompatibilityWriterPort legacy)
        => new(
            new TransHelperUnitOfWork(),
            _idempotencyRepo,
            _movementRepo,
            _positionRepo,
            _scopeRepo,
            legacy);

    private static StockConsequenceDraft BuildReceiptDraft(CaseIds ids, string? idempotencyKey = null)
    {
        var item = new BrgReff(ids.BrgId, "Item S8");
        var receiptSource = ReceiptSourceType.Key(ids.DoId);
        var location = LayananType.Key(ids.LocationId);
        var movementKey = StockMovementModel.Key(ids.MovementId);

        var layer = StockLayerModel.Create(
            item,
            receiptSource,
            location,
            movementKey,
            initialQuantity: 10m,
            UnitValuationType.Create(1000m),
            BusinessTime,
            StockFactOriginEnum.Native,
            expirationDate: new DateOnly(2027, 6, 30),
            batch: "S8-BATCH",
            stockLayerId: ids.LayerId);

        var line = StockMovementLineType.Create(
            1,
            item,
            receiptSource,
            location,
            StockMovementDirectionEnum.Inbound,
            10m,
            UnitValuationType.Create(1000m),
            StockFactOriginEnum.Native,
            StockLayerModel.Key(ids.LayerId));

        var movement = StockMovementModel.CreateReceipt(
            SourceTransactionReferenceType.Key(ids.SourceTxId),
            BusinessTime,
            [line],
            StockFactOriginEnum.Native,
            stockMovementId: ids.MovementId);

        var position = StockPositionModel.CreateEmpty(item, receiptSource, location)
            .AddLayer(layer);

        var scope = StockLedgerScopeStateModel.CreateNotReconstructed(
            StockLedgerScopeKeyType.Create(ids.BrgId, ids.DoId));

        var legacyWrite = new LegacyCompatibilityWriteRequest(
            SourceTransactionReferenceType.Key(ids.SourceTxId),
            StockMovementKindEnum.Receipt,
            StockLedgerScopeKeyType.Create(ids.BrgId, ids.DoId),
            [
                new LegacyCompatibilityBalanceMutationType(
                    LegacyBalanceMutationActionEnum.Upsert,
                    ids.BrgId,
                    ids.DoId,
                    ids.LocationId,
                    Quantity: 10m,
                    UnitCost: 1000m,
                    ExpirationDate: new DateOnly(2027, 6, 30),
                    Batch: "S8-BATCH",
                    PurchaseOrderId: null,
                    LegacyRowId: null)
            ],
            Array.Empty<LegacyCompatibilityJournalEntryType>());

        return new StockConsequenceDraft(
            idempotencyKey ?? ids.IdempotencyKey,
            ProcessedAt,
            movement,
            [position],
            scope,
            legacyWrite);
    }

    private static CaseIds CreateCaseIds(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 1 ? tag : tag[..1];
        // Column widths: BrgId VARCHAR(13), ReceiptSourceId VARCHAR(10), LayananId VARCHAR(5).
        return new CaseIds(
            IdempotencyKey: $"S8-IDEM-{tag}-{ulid}",
            SourceTxId: $"S8-SRC-{tag}-{ulid}",
            MovementId: ulid,
            LayerId: Ulid.NewUlid().ToString(),
            BrgId: $"BRGS8{shortTag}{ulid}"[..13],
            DoId: $"DOS8{shortTag}{ulid}"[..10],
            LocationId: $"G{shortTag}{ulid}"[..5]);
    }

    private static void Cleanup(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        conn.Execute(
            """
            DELETE FROM BILRG_StokMovementLine WHERE StockMovementId = @MovementId;
            DELETE FROM BILRG_StokMovement WHERE StockMovementId = @MovementId;
            DELETE FROM BILRG_StokLayer WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokPosition WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokLedgerScope WHERE BrgId = @BrgId AND ReceiptSourceId = @DoId;
            DELETE FROM BILRG_StokSourceIdempotency WHERE IdempotencyKey = @IdempotencyKey;
            """,
            new
            {
                ids.MovementId,
                ids.BrgId,
                ids.DoId,
                ids.IdempotencyKey
            });
    }

    private sealed record CaseIds(
        string IdempotencyKey,
        string SourceTxId,
        string MovementId,
        string LayerId,
        string BrgId,
        string DoId,
        string LocationId);
}
