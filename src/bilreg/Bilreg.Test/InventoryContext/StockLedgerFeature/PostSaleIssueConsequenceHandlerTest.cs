using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class PostSaleIssueConsequenceHandlerTest
{
    private readonly Mock<ILegacyStockReadPort> _legacyRead = new();
    private readonly Mock<IStockConsequenceUnitOfWork> _uow = new();

    private PostSaleIssueConsequenceHandler CreateHandler() =>
        new(_legacyRead.Object, _uow.Object);

    [Fact]
    public async Task UT1_GivenTrsReffIdOver10Chars_WhenHandle_ThenThrowsArgumentException()
    {
        var cmd = new PostSaleIssueConsequenceCommand(
            "BRG-01", "LYN-01", 5m, "TR-1234567890", DateTime.Today, "USR-01", SaleIssueKindEnum.Db);

        var act = () => CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UT2_GivenSufficientStock_WhenHandle_ThenAllocatesFefoAndCommitsLegacyOpsWithProvenance()
    {
        var ed1 = new DateTime(2026, 6, 1);
        var ed2 = new DateTime(2026, 12, 1);
        _legacyRead
            .Setup(x => x.ListJournalsBySource("SL-001", "BRG-01", "LYN-01", null))
            .Returns([]);
        _legacyRead
            .Setup(x => x.ListBalancesAtLocation("BRG-01", "LYN-01"))
            .Returns([
                new LegacyStockBalanceReadModel("STK-2", "BRG-01", "DO-02", "LYN-01", ed2, "B2", 10m, 200m, new DateTime(2026, 1, 2), "PO-02", "TAB"),
                new LegacyStockBalanceReadModel("STK-1", "BRG-01", "DO-01", "LYN-01", ed1, "B1", 3m, 100m, new DateTime(2026, 1, 1), "PO-01", "TAB")
            ]);

        StockConsequenceDraft? captured = null;
        _uow.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>()))
            .Callback<StockConsequenceDraft>(d => captured = d);

        var cmd = new PostSaleIssueConsequenceCommand(
            "BRG-01", "LYN-01", 5m, "SL-001", DateTime.Today, "USR-01", SaleIssueKindEnum.Db);

        var res = await CreateHandler().Handle(cmd, CancellationToken.None);

        res.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Success);
        res.Lines.Should().HaveCount(2);
        res.Lines[0].LegacyStokId.Should().Be("STK-1");
        res.Lines[0].QtyOut.Should().Be(3m);
        res.Lines[0].PoReffId.Should().Be("PO-01");
        res.Lines[0].NoBatch.Should().Be("B1");
        res.Lines[0].TglEd.Should().Be(ed1);
        res.Lines[0].Hpp.Should().Be(100m);

        res.Lines[1].LegacyStokId.Should().Be("STK-2");
        res.Lines[1].QtyOut.Should().Be(2m);
        res.Lines[1].PoReffId.Should().Be("PO-02");

        captured.Should().NotBeNull();
        captured!.LegacyOperations.Should().HaveCount(4);
        captured.LegacyOperations.OfType<LegacyOutboundBukuWriteOperation>().Should().HaveCount(2);
        captured.LegacyOperations.OfType<LegacyStokDepleteWriteOperation>().Should().HaveCount(2);
    }

    [Fact]
    public async Task UT3_GivenPriorMatchingJournals_WhenHandle_ThenReturnsIdempotentWithoutCommit()
    {
        _legacyRead
            .Setup(x => x.ListJournalsBySource("SL-001", "BRG-01", "LYN-01", null))
            .Returns([
                new LegacyStockJournalReadModel("BK-1", "BRG-01", "DO-01", "LYN-01", "PO-01", "B1", 0m, 5m, 100m, "DB", "SL-001", DateTime.Today, new DateTime(2026, 6, 1), "TAB")
            ]);

        var cmd = new PostSaleIssueConsequenceCommand(
            "BRG-01", "LYN-01", 5m, "SL-001", DateTime.Today, "USR-01", SaleIssueKindEnum.Db);

        var res = await CreateHandler().Handle(cmd, CancellationToken.None);

        res.Outcome.Should().Be(PostSaleIssueOutcomeEnum.Idempotent);
        res.Lines.Should().ContainSingle();
        res.Lines[0].LegacyBukuId.Should().Be("BK-1");
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }

    [Fact]
    public async Task UT4_GivenInsufficientStock_WhenHandle_ThenReturnsInsufficientStockWithoutCommit()
    {
        _legacyRead
            .Setup(x => x.ListJournalsBySource("SL-001", "BRG-01", "LYN-01", null))
            .Returns([]);
        _legacyRead
            .Setup(x => x.ListBalancesAtLocation("BRG-01", "LYN-01"))
            .Returns([
                new LegacyStockBalanceReadModel("STK-1", "BRG-01", "DO-01", "LYN-01", new DateTime(2026, 6, 1), "B1", 2m, 100m, new DateTime(2026, 1, 1), "PO-01", "TAB")
            ]);

        var cmd = new PostSaleIssueConsequenceCommand(
            "BRG-01", "LYN-01", 5m, "SL-001", DateTime.Today, "USR-01", SaleIssueKindEnum.Db);

        var res = await CreateHandler().Handle(cmd, CancellationToken.None);

        res.Outcome.Should().Be(PostSaleIssueOutcomeEnum.InsufficientStock);
        res.ShortfallQty.Should().Be(3m);
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }
}
