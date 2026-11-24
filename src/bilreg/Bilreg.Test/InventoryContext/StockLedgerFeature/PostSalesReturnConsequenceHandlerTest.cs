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
public class PostSalesReturnConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;

    private const string BrgId = "BRGSTLR20001";
    private const string DoId = "DOSTLR201";
    private const string LayananId = "LY001";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 6, 10, 11, 0, 0);
    private static readonly DateTime TglEd = new(2028, 1, 15);

    public PostSalesReturnConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
    }

    [Theory]
    [InlineData(SalesReturnKindEnum.Rj, "RJ")]
    [InlineData(SalesReturnKindEnum.Ru, "RU")]
    [InlineData(SalesReturnKindEnum.Rt, "RT")]
    public async Task Handle_Success_WritesLegacyBukuAndStok(SalesReturnKindEnum kind, string expectedLegacyKind)
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var trsId = $"TRR{expectedLegacyKind}001";
        var sut = CreateHandler();
        var cmd = new PostSalesReturnConsequenceCommand(
            BrgId, DoId, LayananId, 3, 150m, trsId, TglMutasi, UserId, kind, TglEd, "B01");

        var result = await sut.Handle(cmd, default);

        result.Outcome.Should().Be(PostSalesReturnOutcomeEnum.Success);
        result.LegacyBukuId.Should().HaveLength(10).And.StartWith("BK");
        result.LegacyStokId.Should().HaveLength(10).And.StartWith("ST");

        _legacyRead.ListJournalsBySource(trsId, BrgId, LayananId, expectedLegacyKind)
            .Should().ContainSingle(x => x.LegacyBukuId == result.LegacyBukuId && x.QtyIn == 3);
        _legacyRead.ListBalances(BrgId, DoId)
            .Should().Contain(x => x.LegacyStokId == result.LegacyStokId && x.QtySisa == 3);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_DoesNotDoubleQty()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        var sut = CreateHandler();
        var cmd = new PostSalesReturnConsequenceCommand(
            BrgId, DoId, LayananId, 5, 100m, "TRSR20002", TglMutasi, UserId, SalesReturnKindEnum.Rj, TglEd);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostSalesReturnOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostSalesReturnOutcomeEnum.Idempotent);
        second.LegacyBukuId.Should().Be(first.LegacyBukuId);

        _legacyRead.ListJournalsBySource("TRSR20002", BrgId, LayananId, "RJ")
            .Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_TrsReffIdOver10Chars_ThrowsArgumentException()
    {
        var sut = CreateHandler();
        var cmd = new PostSalesReturnConsequenceCommand(
            BrgId, DoId, LayananId, 1, 100m, "TRSR2000123", TglMutasi, UserId, SalesReturnKindEnum.Rj, TglEd);

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private PostSalesReturnConsequenceHandler CreateHandler() =>
        new(_legacyRead, new LegacyOnlyUow(_writer));

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
