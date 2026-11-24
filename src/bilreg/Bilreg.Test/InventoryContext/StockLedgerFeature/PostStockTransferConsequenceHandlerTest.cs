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
public class PostStockTransferConsequenceHandlerTest
{
    private readonly IOptions<DatabaseOptions> _db = ConnStringHelper.GetTestEnv();
    private readonly LegacyStockWriterPort _writer;
    private readonly LegacyStockReadPort _legacyRead;

    private const string BrgId = "BRGSTLT20001";
    private const string SrcLayananId = "LY001";
    private const string DstLayananId = "LY002";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 6, 5, 10, 0, 0);

    public PostStockTransferConsequenceHandlerTest()
    {
        _writer = new LegacyStockWriterPort(_db);
        _legacyRead = new LegacyStockReadPort(_db);
    }

    [Fact]
    public async Task Handle_Success_MultiRowFefoAllocation_DepletesSourceAndCreatesDestStok()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        _writer.InsertInbound(new LegacyInboundWriteRequest(
            BrgId, "DOSTLT201", SrcLayananId, 5, 100m,
            new DateTime(2027, 1, 1), TglMutasi.AddDays(-5), TglMutasi.AddDays(-5),
            "DOSTLT201", "DO", NoBatch: "B1", SatuanId: "TAB"));
        _writer.InsertInbound(new LegacyInboundWriteRequest(
            BrgId, "DOSTLT202", SrcLayananId, 8, 120m,
            new DateTime(2027, 6, 1), TglMutasi.AddDays(-3), TglMutasi.AddDays(-3),
            "DOSTLT202", "DO", NoBatch: "B2", SatuanId: "TAB"));

        var sut = CreateHandler();
        var cmd = new PostStockTransferConsequenceCommand(
            BrgId, 9, SrcLayananId, DstLayananId, "TRST20001", TglMutasi, UserId);

        var result = await sut.Handle(cmd, default);

        result.Outcome.Should().Be(PostStockTransferOutcomeEnum.Success);
        result.Lines.Should().HaveCount(2);

        var srcBalances = _legacyRead.ListBalancesAtLocation(BrgId, SrcLayananId);
        srcBalances.Sum(x => x.QtySisa).Should().Be(4);

        var dstBalances = _legacyRead.ListBalancesAtLocation(BrgId, DstLayananId);
        dstBalances.Sum(x => x.QtySisa).Should().Be(9);
        dstBalances.Should().Contain(x => x.BrgMasukReffId == "DOSTLT201" && x.QtySisa == 5 && x.Hpp == 100m);
        dstBalances.Should().Contain(x => x.BrgMasukReffId == "DOSTLT202" && x.QtySisa == 4 && x.Hpp == 120m);

        _legacyRead.ListJournalsBySource("TRST20001", BrgId, SrcLayananId, "MT_OUT")
            .Sum(x => x.QtyOut).Should().Be(9);
        _legacyRead.ListJournalsBySource("TRST20001", BrgId, DstLayananId, "MT_IN")
            .Sum(x => x.QtyIn).Should().Be(9);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_ReturnsRecordedLegsWithoutDoubleTransfer()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        _writer.InsertInbound(new LegacyInboundWriteRequest(
            BrgId, "DOSTLT203", SrcLayananId, 10, 100m,
            new DateTime(2027, 3, 1), TglMutasi.AddDays(-2), TglMutasi.AddDays(-2),
            "DOSTLT203", "DO"));

        var sut = CreateHandler();
        var cmd = new PostStockTransferConsequenceCommand(
            BrgId, 4, SrcLayananId, DstLayananId, "TRST20002", TglMutasi, UserId);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostStockTransferOutcomeEnum.Success);

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostStockTransferOutcomeEnum.Idempotent);
        second.Lines.Should().ContainSingle();
        second.Lines[0].LegacyBukuOutId.Should().Be(first.Lines[0].LegacyBukuOutId);
        second.Lines[0].LegacyBukuInId.Should().Be(first.Lines[0].LegacyBukuInId);

        _legacyRead.ListBalancesAtLocation(BrgId, SrcLayananId).Sum(x => x.QtySisa).Should().Be(6);
        _legacyRead.ListBalancesAtLocation(BrgId, DstLayananId).Sum(x => x.QtySisa).Should().Be(4);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsInsufficientWithShortfall()
    {
        StockLedgerV2SchemaFixture.EnsureSchema();
        using var trans = TransHelper.NewScope();

        _writer.InsertInbound(new LegacyInboundWriteRequest(
            BrgId, "DOSTLT204", SrcLayananId, 3, 100m,
            new DateTime(2027, 3, 1), TglMutasi.AddDays(-2), TglMutasi.AddDays(-2),
            "DOSTLT204", "DO"));

        var sut = CreateHandler();
        var cmd = new PostStockTransferConsequenceCommand(
            BrgId, 10, SrcLayananId, DstLayananId, "TRST20003", TglMutasi, UserId);

        var result = await sut.Handle(cmd, default);

        result.Outcome.Should().Be(PostStockTransferOutcomeEnum.InsufficientStock);
        result.ShortfallQty.Should().Be(7);
        _legacyRead.ListBalancesAtLocation(BrgId, SrcLayananId).Sum(x => x.QtySisa).Should().Be(3);
    }

    [Fact]
    public async Task Handle_TrsReffIdOver10Chars_ThrowsArgumentException()
    {
        var sut = CreateHandler();
        var cmd = new PostStockTransferConsequenceCommand(
            BrgId, 1, SrcLayananId, DstLayananId, "TRST2000123", TglMutasi, UserId);

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private PostStockTransferConsequenceHandler CreateHandler() =>
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
