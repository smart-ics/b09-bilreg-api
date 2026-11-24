using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

[Collection(StockLedgerSqlCollection.Name)]
public class PostGoodsReceiptConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;

    private const string BrgId = "BRGSTLD20001";
    private const string DoId = "DOSTLD201";
    private const string LayananId = "LY001";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 6, 1, 9, 0, 0);
    private static readonly DateTime TglEd = new(2028, 6, 15);

    public PostGoodsReceiptConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
    }

    [Fact]
    public async Task Handle_Success_WritesLegacyBukuAndStok()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateHandler();
        var cmd = NewCommand(trsReffId: "TRSD20001", qty: 10, hpp: 100m);

        var result = await sut.Handle(cmd, default);

        result.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
        result.LegacyBukuId.Should().HaveLength(10).And.StartWith("BK");
        result.LegacyStokId.Should().HaveLength(10).And.StartWith("ST");

        _legacyRead.ListJournals(BrgId, DoId)
            .Should().Contain(x => x.LegacyBukuId == result.LegacyBukuId && x.QtyIn == 10);
        _legacyRead.ListBalances(BrgId, DoId)
            .Should().Contain(x => x.LegacyStokId == result.LegacyStokId && x.QtySisa == 10);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_DoesNotDoubleQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateHandler();
        var cmd = NewCommand(trsReffId: "TRSD20002", qty: 7, hpp: 50m);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Idempotent);
        second.LegacyBukuId.Should().Be(first.LegacyBukuId);

        _legacyRead.ListJournalsBySource("TRSD20002", BrgId, LayananId, "DO")
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_AllowAdditiveLine_AppendsSecondLine()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateHandler();
        var first = await sut.Handle(NewCommand(trsReffId: "TRSD20003", qty: 10, hpp: 100m), default);
        first.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);

        var bonus = NewCommand(trsReffId: "TRSD20003", qty: 2, hpp: 0m) with { AllowAdditiveLine = true };
        var second = await sut.Handle(bonus, default);
        second.Outcome.Should().Be(PostGoodsReceiptOutcomeEnum.Success);
        second.LegacyBukuId.Should().NotBe(first.LegacyBukuId);

        var journals = _legacyRead.ListJournalsBySource("TRSD20003", BrgId, LayananId, "DO");
        journals.Should().HaveCount(2);
        journals.Sum(x => x.QtyIn).Should().Be(12);
    }

    [Fact]
    public async Task Handle_TrsReffIdOver10Chars_ThrowsArgumentException()
    {
        var sut = CreateHandler();
        var cmd = NewCommand(trsReffId: "TRSD2000123", qty: 5, hpp: 10m);

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private PostGoodsReceiptConsequenceHandler CreateHandler() =>
        new(_legacyRead, new LegacyOnlyUow(_writer));

    private static PostGoodsReceiptConsequenceCommand NewCommand(
        string trsReffId,
        decimal qty,
        decimal hpp) =>
        new(
            BrgId,
            DoId,
            LayananId,
            qty,
            hpp,
            trsReffId,
            TglMutasi,
            UserId,
            TglEd: TglEd,
            PoReffId: "PO001",
            NoBatch: "B01",
            SatuanId: "TAB");

    private sealed class LegacyOnlyUow(ILegacyStockWriterPort writer) : IStockConsequenceUnitOfWork
    {
        public void Commit(StockConsequenceDraft draft)
        {
            foreach (var op in draft.LegacyOperations)
            {
                switch (op)
                {
                    case LegacyInboundWriteOperation inbound:
                        writer.InsertInbound(inbound.Request);
                        break;
                    case LegacyOutboundBukuWriteOperation outbound:
                        writer.InsertOutboundBuku(outbound.Request);
                        break;
                    case LegacyStokDepleteWriteOperation deplete:
                        writer.DepleteStok(deplete.Request);
                        break;
                    case LegacyReverseBukuWriteOperation reverse:
                        writer.InsertReverseBuku(reverse.Request);
                        break;
                    case LegacyStokRestoreWriteOperation restore:
                        writer.RestoreStok(restore.Request);
                        break;
                }
            }
        }
    }
}
