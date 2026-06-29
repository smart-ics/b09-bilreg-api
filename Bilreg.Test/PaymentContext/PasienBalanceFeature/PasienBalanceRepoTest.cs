using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.PaymentContext.PasienBalanceFeature;

public class PasienBalanceRepoTest
{
    private const string PasienId = "112233400000821";
    private static readonly DateTime TrsDate = new(2026, 6, 29, 10, 0, 0);

    private readonly Mock<IBilrgTataRekPasienBalanceDal> _headerDalMock;
    private readonly Mock<IBilrgTataRekPasienBalanceHistoryDal> _historyDalMock;
    private readonly PasienBalanceRepo _sut;

    public PasienBalanceRepoTest()
    {
        _headerDalMock = new Mock<IBilrgTataRekPasienBalanceDal>();
        _historyDalMock = new Mock<IBilrgTataRekPasienBalanceHistoryDal>();
        _sut = new PasienBalanceRepo(_headerDalMock.Object, _historyDalMock.Object);
    }

    private static BilrgTataRekPasienBalanceDto BuildHeaderDto(
        decimal currentJasaBalance = 0m,
        decimal currentObatBalance = 0m,
        int version = 0,
        string lastHistoryId = "") =>
        new(
            PasienId,
            currentJasaBalance,
            currentObatBalance,
            lastHistoryId,
            TrsDate,
            version,
            "kasir-01",
            TrsDate,
            "kasir-01",
            TrsDate,
            string.Empty,
            new DateTime(3000, 1, 1));

    [Fact]
    public void GivenNewPasienBalance_WhenSaveChanges_ThenInsertHeaderAndPendingHistory()
    {
        var model = PasienBalanceModel.Create(PasienId);
        model.ApplyCharge(100_000m, 50_000m, "RG-001", TrsDate, "Charge", "kasir-01");

        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IPasienKey>()))
            .Returns((BilrgTataRekPasienBalanceDto)null!);

        _sut.SaveChanges(model);

        _headerDalMock.Verify(
            x => x.Insert(It.Is<BilrgTataRekPasienBalanceDto>(d =>
                d.PasienId == PasienId && d.CurrentJasaBalance == 100_000m && d.CurrentObatBalance == 50_000m)),
            Times.Once);
        _headerDalMock.Verify(x => x.UpdateConditional(It.IsAny<BilrgTataRekPasienBalanceDto>(), It.IsAny<int>()), Times.Never);
        _historyDalMock.Verify(
            x => x.Insert(It.Is<IEnumerable<BilrgTataRekPasienBalanceHistoryDto>>(rows => rows.Count() == 1)),
            Times.Once);
    }

    [Fact]
    public void GivenExistingPasienBalance_WhenSaveChanges_ThenUpdateConditionalAndInsertNewHistoryOnly()
    {
        var model = PasienBalanceModel.Create(PasienId);
        model.ApplyCharge(100_000m, 50_000m, "RG-001", TrsDate, "Charge", "kasir-01");

        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IPasienKey>()))
            .Returns(BuildHeaderDto());

        _headerDalMock
            .Setup(x => x.UpdateConditional(It.IsAny<BilrgTataRekPasienBalanceDto>(), 0))
            .Returns(1);

        _sut.SaveChanges(model);

        _headerDalMock.Verify(x => x.Insert(It.IsAny<BilrgTataRekPasienBalanceDto>()), Times.Never);
        _headerDalMock.Verify(x => x.UpdateConditional(It.IsAny<BilrgTataRekPasienBalanceDto>(), 0), Times.Once);
        _historyDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BilrgTataRekPasienBalanceHistoryDto>>()), Times.Once);
        model.Version.Should().Be(1);
    }

    [Fact]
    public void GivenStaleVersion_WhenSaveChanges_ThenShouldThrow()
    {
        var model = PasienBalanceModel.Create(PasienId);
        model.ApplyCharge(100_000m, 0m, "RG-001", TrsDate, "Charge", "kasir-01");

        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IPasienKey>()))
            .Returns(BuildHeaderDto(version: 0));

        _headerDalMock
            .Setup(x => x.UpdateConditional(It.IsAny<BilrgTataRekPasienBalanceDto>(), 0))
            .Returns(0);

        Action act = () => _sut.SaveChanges(model);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*stale*");
    }

    [Fact]
    public void GivenHeaderExists_WhenLoadEntity_ThenHydratesHistory()
    {
        var header = BuildHeaderDto(currentJasaBalance: 100_000m, currentObatBalance: 50_000m, lastHistoryId: "PBH001");
        var historyDto = new BilrgTataRekPasienBalanceHistoryDto(
            "PBH001", PasienId, "RG-001", TrsDate,
            0m, 0m, 100_000m, 50_000m, 0m, 0m, 100_000m, 50_000m,
            "Charge", TrsDate, "kasir-01");

        _headerDalMock.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(header);
        _historyDalMock
            .Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns([historyDto]);

        var result = _sut.LoadEntity(PasienBalanceModel.Key(PasienId));

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.PasienId.Should().Be(PasienId);
                model.CurrentJasaBalance.Should().Be(100_000m);
                model.CurrentObatBalance.Should().Be(50_000m);
                model.CurrentBalance.Should().Be(150_000m);
                model.ListHistory.Should().HaveCount(1);
                model.ListHistory.Single().IsPersisted.Should().BeTrue();
            },
            onNone: () => throw new InvalidOperationException("Expected Some"));
    }

    [Fact]
    public void GivenHistoryExists_WhenListHistory_ThenReturnsOrderedHistory()
    {
        var historyDto = new BilrgTataRekPasienBalanceHistoryDto(
            "PBH001", PasienId, "RG-001", TrsDate,
            0m, 0m, 100_000m, 50_000m, 0m, 0m, 100_000m, 50_000m,
            "Charge", TrsDate, "kasir-01");

        _historyDalMock
            .Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns([historyDto]);

        var result = _sut.ListHistory(PasienBalanceModel.Key(PasienId)).ToList();

        result.Should().HaveCount(1);
        result[0].HistoryId.Should().Be("PBH001");
        result[0].ClosingJasaBalance.Should().Be(100_000m);
        result[0].ClosingObatBalance.Should().Be(50_000m);
        result[0].ClosingBalance.Should().Be(150_000m);
    }

    [Fact]
    public void GivenMissingHeader_WhenLoadEntity_ThenReturnsNone()
    {
        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IPasienKey>()))
            .Returns((BilrgTataRekPasienBalanceDto)null!);

        var result = _sut.LoadEntity(PasienBalanceModel.Key(PasienId));

        result.HasValue.Should().BeFalse();
    }
}
