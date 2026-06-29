using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.PaymentContext.PasienBalanceFeature;

public class PasienBalanceBootstrapTest
{
    private const string PasienId = "112233400000821";
    private static readonly DateTime TrsDate1 = new(2026, 6, 29, 10, 0, 0);
    private static readonly DateTime TrsDate2 = new(2026, 6, 30, 9, 0, 0);

    [Fact]
    public void GivenLegacyRows_WhenBootstrap_ThenShouldMapOutstandingEntries()
    {
        var legacyReaderMock = new Mock<IPasienBalanceLegacyReader>();
        legacyReaderMock
            .Setup(x => x.ListOutstanding(It.IsAny<PasienBalanceModel>()))
            .Returns(
            [
                new LegacyOutstandingReceivable(PasienId, "RG-007", 300_000m, 200_000m, TrsDate1, "PIU007"),
                new LegacyOutstandingReceivable(PasienId, "RG-008", 150_000m, 100_000m, TrsDate2, "PIU008"),
                new LegacyOutstandingReceivable(PasienId, "RG-009", 0m, 0m, TrsDate2, "PIU009")
            ]);

        var sut = new PasienBalanceBootstrapService(legacyReaderMock.Object);
        var model = sut.Bootstrap(PasienBalanceModel.Key(PasienId));

        model.PasienId.Should().Be(PasienId);
        model.OutstandingEntries.Should().HaveCount(2);
        model.TotalOutstanding.Should().Be(750_000m);
        model.OutstandingEntries.Should().Contain(x => x.RegId == "RG-007" && x.SourceReference == "PIU007");
        model.OutstandingEntries.Should().OnlyContain(x => x.CreatedBy == PasienBalanceBootstrapService.BootstrapUser);
    }

    [Fact]
    public void GivenNoLegacyRows_WhenBootstrap_ThenShouldReturnEmptyAggregate()
    {
        var legacyReaderMock = new Mock<IPasienBalanceLegacyReader>();
        legacyReaderMock
            .Setup(x => x.ListOutstanding(It.IsAny<PasienBalanceModel>()))
            .Returns([]);

        var sut = new PasienBalanceBootstrapService(legacyReaderMock.Object);
        var model = sut.Bootstrap(PasienBalanceModel.Key(PasienId));

        model.OutstandingEntries.Should().BeEmpty();
        model.TotalOutstanding.Should().Be(0m);
    }
}
