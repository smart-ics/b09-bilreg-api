using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class StockSourceIdempotencyRepoTest
{
    private readonly StockSourceIdempotencyRepo _sut = new(
        new StockSourceIdempotencyDal(ConnStringHelper.GetTestEnv()));

    private static readonly DateTime ProcessedAt = new(2026, 8, 7, 11, 30, 0);

    [Fact]
    public void InsertOrGetExisting_FirstInsert_WasInsertedTrue()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var model = BuildModel("SRC-CONSEQ-S6-001");
        var result = _sut.InsertOrGetExisting(model);

        result.WasInserted.Should().BeTrue();
        result.Record.IdempotencyId.Should().Be(model.IdempotencyId);
        result.Record.IdempotencyKey.Should().Be("SRC-CONSEQ-S6-001");
        result.Record.IdempotencyKind.Should().Be(StockSourceIdempotencyKindEnum.SourceConsequence);
    }

    [Fact]
    public void InsertOrGetExisting_DuplicateBusinessKey_ReturnsExistingWithoutMutation()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var first = BuildModel("SRC-CONSEQ-S6-002", stockMovementId: "MOV-FIRST");
        var firstResult = _sut.InsertOrGetExisting(first);
        firstResult.WasInserted.Should().BeTrue();

        var duplicate = BuildModel(
            "SRC-CONSEQ-S6-002",
            stockMovementId: "MOV-SECOND",
            idempotencyId: Ulid.NewUlid().ToString());
        var secondResult = _sut.InsertOrGetExisting(duplicate);

        secondResult.WasInserted.Should().BeFalse();
        secondResult.Record.IdempotencyId.Should().Be(first.IdempotencyId);
        secondResult.Record.StockMovementId.Should().Be("MOV-FIRST");
        secondResult.Record.StockMovementId.Should().NotBe("MOV-SECOND");

        var byKey = _sut.LoadByBusinessKey(
            StockSourceIdempotencyModel.BusinessKey(
                StockSourceIdempotencyKindEnum.SourceConsequence,
                "SRC-CONSEQ-S6-002"));
        byKey.HasValue.Should().BeTrue();
        byKey.Value.IdempotencyId.Should().Be(first.IdempotencyId);
    }

    [Fact]
    public void LoadEntity_BySurrogateId_RoundTrips()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var model = BuildModel("SRC-CONSEQ-S6-003");
        _sut.InsertOrGetExisting(model);

        var loaded = _sut.LoadEntity(model);
        loaded.HasValue.Should().BeTrue();
        loaded.Value.SourceTransactionId.Should().Be("SRC-TX-S6");
        loaded.Value.BrgId.Should().Be("BRGS606");
        loaded.Value.ReceiptSourceId.Should().Be("DO6006");
        loaded.Value.ProcessedAt.Should().Be(ProcessedAt);
    }

    private static StockSourceIdempotencyModel BuildModel(
        string key,
        string stockMovementId = "MOV-S6-IDEM",
        string? idempotencyId = null)
        => StockSourceIdempotencyModel.Create(
            StockSourceIdempotencyKindEnum.SourceConsequence,
            key,
            ProcessedAt,
            sourceTransactionId: "SRC-TX-S6",
            stockMovementId: stockMovementId,
            brgId: "BRGS606",
            receiptSourceId: "DO6006",
            idempotencyId: idempotencyId);
}
