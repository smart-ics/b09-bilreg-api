using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class PostSaleVoidConsequenceHandlerTest
{
    private readonly Mock<ILegacyStockReadPort> _legacyRead = new();
    private readonly Mock<IStockConsequenceUnitOfWork> _uow = new();

    private PostSaleVoidConsequenceHandler CreateHandler() =>
        new(_legacyRead.Object, _uow.Object);

    [Fact]
    public async Task UT1_GivenVoidTrsReffIdOver10Chars_WhenHandle_ThenThrowsArgumentException()
    {
        var cmd = new PostSaleVoidConsequenceCommand(
            "SL-001", "VD-1234567890", DateTime.Today, "USR-01");

        var act = () => CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UT2_GivenOriginalSaleBukuRows_WhenHandle_ThenCommitsReverseBuku11AndRestoreStok()
    {
        _legacyRead.Setup(x => x.ListJournalsBySource("VD-001", null, null, null)).Returns([]);
        _legacyRead.Setup(x => x.ListJournalsBySource("SL-001", null, null, null)).Returns([
            new LegacyStockJournalReadModel("BK-1", "BRG-01", "DO-01", "LYN-01", "PO-01", "B1", 0m, 4m, 100m, "DB", "SL-001", DateTime.Today, new DateTime(2026, 6, 1), "TAB")
        ]);

        StockConsequenceDraft? captured = null;
        _uow.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>()))
            .Callback<StockConsequenceDraft>(d => captured = d);

        var cmd = new PostSaleVoidConsequenceCommand("SL-001", "VD-001", DateTime.Today, "USR-01");

        var res = await CreateHandler().Handle(cmd, CancellationToken.None);

        res.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Success);
        res.Lines.Should().ContainSingle();
        res.Lines[0].QtyRestored.Should().Be(4m);
        captured!.LegacyOperations.OfType<LegacyReverseBukuWriteOperation>()
            .Single().Request.MovementKindString.Should().Be("11");
        captured.LegacyOperations.OfType<LegacyStokRestoreWriteOperation>()
            .Single().Request.QtyIn.Should().Be(4m);
    }

    [Fact]
    public async Task UT3_GivenNoOriginalBukuRows_WhenHandle_ThenReturnsOriginalNotFound()
    {
        _legacyRead.Setup(x => x.ListJournalsBySource("VD-001", null, null, null)).Returns([]);
        _legacyRead.Setup(x => x.ListJournalsBySource("SL-001", null, null, null)).Returns([]);

        var cmd = new PostSaleVoidConsequenceCommand("SL-001", "VD-001", DateTime.Today, "USR-01");

        var res = await CreateHandler().Handle(cmd, CancellationToken.None);

        res.Outcome.Should().Be(PostSaleVoidOutcomeEnum.OriginalNotFound);
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }

    [Fact]
    public async Task UT4_GivenPriorVoidBukuRows_WhenHandle_ThenReturnsIdempotent()
    {
        _legacyRead.Setup(x => x.ListJournalsBySource("VD-001", null, null, null)).Returns([
            new LegacyStockJournalReadModel("BK-2", "BRG-01", "DO-01", "LYN-01", "PO-01", "B1", 4m, 0m, 100m, "11", "VD-001", DateTime.Today, new DateTime(2026, 6, 1), "TAB")
        ]);

        var cmd = new PostSaleVoidConsequenceCommand("SL-001", "VD-001", DateTime.Today, "USR-01");

        var res = await CreateHandler().Handle(cmd, CancellationToken.None);

        res.Outcome.Should().Be(PostSaleVoidOutcomeEnum.Idempotent);
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }
}
