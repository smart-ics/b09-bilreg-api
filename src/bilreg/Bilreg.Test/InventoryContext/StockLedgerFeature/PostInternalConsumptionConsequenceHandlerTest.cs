using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class PostInternalConsumptionConsequenceHandlerTest
{
    private readonly Mock<ILegacyStockReadPort> _legacyRead = new();
    private readonly Mock<IStockConsequenceUnitOfWork> _uow = new();

    private PostInternalConsumptionConsequenceHandler CreateHandler() =>
        new(_legacyRead.Object, _uow.Object);

    [Fact]
    public async Task UT1_GivenTrsReffIdOver10Chars_WhenHandle_ThenThrowsArgumentException()
    {
        var cmd = new PostInternalConsumptionConsequenceCommand(
            "BRG-01", "LYN-01", 2m, "PK-1234567890", DateTime.Today, "USR-01");

        var act = () => CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UT2_GivenSufficientStock_WhenHandle_ThenCommitsPkOutboundAndDeplete()
    {
        var ed = new DateTime(2026, 8, 1);
        _legacyRead
            .Setup(x => x.ListJournalsBySource("PK-001", "BRG-01", "LYN-01", null))
            .Returns([]);
        _legacyRead
            .Setup(x => x.ListBalancesAtLocation("BRG-01", "LYN-01"))
            .Returns([
                new LegacyStockBalanceReadModel("STK-1", "BRG-01", "DO-01", "LYN-01", ed, "B1", 5m, 100m, new DateTime(2026, 1, 1), "PO-01", "TAB")
            ]);

        StockConsequenceDraft? captured = null;
        _uow.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>()))
            .Callback<StockConsequenceDraft>(d => captured = d);

        var cmd = new PostInternalConsumptionConsequenceCommand(
            "BRG-01", "LYN-01", 2m, "PK-001", DateTime.Today, "USR-01");

        var res = await CreateHandler().Handle(cmd, CancellationToken.None);

        res.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Success);
        res.Lines.Should().ContainSingle();
        res.Lines[0].QtyOut.Should().Be(2m);
        res.Lines[0].StokCurrent.Should().Be(3m);
        captured!.LegacyOperations.OfType<LegacyOutboundBukuWriteOperation>()
            .Single().Request.MovementKindString.Should().Be("PK");
        captured.LegacyOperations.OfType<LegacyStokDepleteWriteOperation>().Should().ContainSingle();
    }

    [Fact]
    public async Task UT3_GivenPriorMatchingJournals_WhenHandle_ThenReturnsIdempotent()
    {
        _legacyRead
            .Setup(x => x.ListJournalsBySource("PK-001", "BRG-01", "LYN-01", null))
            .Returns([
                new LegacyStockJournalReadModel("BK-1", "BRG-01", "DO-01", "LYN-01", "PO-01", "B1", 0m, 2m, 100m, "PK", "PK-001", DateTime.Today, new DateTime(2026, 8, 1), "TAB")
            ]);

        var cmd = new PostInternalConsumptionConsequenceCommand(
            "BRG-01", "LYN-01", 2m, "PK-001", DateTime.Today, "USR-01");

        var res = await CreateHandler().Handle(cmd, CancellationToken.None);

        res.Outcome.Should().Be(PostInternalConsumptionOutcomeEnum.Idempotent);
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }
}
