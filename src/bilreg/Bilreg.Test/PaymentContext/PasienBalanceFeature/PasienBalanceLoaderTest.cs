using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.PaymentContext.PasienBalanceFeature;

public class PasienBalanceLoaderTest
{
    private const string PasienId = "112233400000821";
    private static readonly DateTime TrsDate = new(2026, 6, 29, 10, 0, 0);

    [Fact]
    public void GivenAggregateExists_WhenLoad_ThenShouldReturnWithoutSave()
    {
        var existing = PasienBalanceModel.Create(PasienId);
        existing.AddOutstanding("RG-001", 100_000m, 50_000m, TrsDate, "PIU001", "kasir-01");

        var repoMock = new Mock<IPasienBalanceRepo>();
        repoMock
            .Setup(x => x.LoadEntity(It.IsAny<PasienBalanceModel>()))
            .Returns(MayBe.From(existing));

        var legacyReaderMock = new Mock<IPasienBalanceLegacyReader>();
        var clock = new Mock<ITglJamProvider>();
        clock.SetupGet(x => x.Now).Returns(TrsDate);
        var sut = new PasienBalanceLoader(
            repoMock.Object,
            new PasienBalanceBootstrapService(legacyReaderMock.Object, clock.Object));

        var result = sut.Load(PasienBalanceModel.Key(PasienId));

        result.Should().BeSameAs(existing);
        legacyReaderMock.Verify(x => x.ListOutstanding(It.IsAny<PasienBalanceModel>(), It.IsAny<DateOnly>()), Times.Never);
        repoMock.Verify(x => x.SaveChanges(It.IsAny<PasienBalanceModel>()), Times.Never);
    }

    [Fact]
    public void GivenAggregateMissing_WhenLoad_ThenShouldBootstrapPersistAndReturn()
    {
        var repoMock = new Mock<IPasienBalanceRepo>();
        repoMock
            .Setup(x => x.LoadEntity(It.IsAny<PasienBalanceModel>()))
            .Returns(MayBe<PasienBalanceModel>.None);

        var legacyReaderMock = new Mock<IPasienBalanceLegacyReader>();
        legacyReaderMock
            .Setup(x => x.ListOutstanding(It.IsAny<PasienBalanceModel>(), It.IsAny<DateOnly>()))
            .Returns(
            [
                new LegacyOutstandingReceivable(PasienId, "RG-001", 100_000m, 50_000m, TrsDate, "PIU001")
            ]);

        PasienBalanceModel? savedModel = null;
        repoMock
            .Setup(x => x.SaveChanges(It.IsAny<PasienBalanceModel>()))
            .Callback<PasienBalanceModel>(m => savedModel = m);

        var clock = new Mock<ITglJamProvider>();
        clock.SetupGet(x => x.Now).Returns(TrsDate);
        var sut = new PasienBalanceLoader(
            repoMock.Object,
            new PasienBalanceBootstrapService(legacyReaderMock.Object, clock.Object));

        var result = sut.Load(PasienBalanceModel.Key(PasienId));

        result.PasienId.Should().Be(PasienId);
        result.OutstandingEntries.Should().HaveCount(1);
        result.TotalOutstanding.Should().Be(150_000m);
        legacyReaderMock.Verify(x => x.ListOutstanding(It.IsAny<PasienBalanceModel>(), It.IsAny<DateOnly>()), Times.Once);
        repoMock.Verify(x => x.SaveChanges(It.IsAny<PasienBalanceModel>()), Times.Once);
        savedModel.Should().BeSameAs(result);
    }
}
