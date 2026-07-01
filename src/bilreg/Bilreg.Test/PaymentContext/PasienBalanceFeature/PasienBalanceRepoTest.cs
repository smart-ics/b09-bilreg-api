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
    private readonly Mock<IBilrgTataRekPasienBalanceOutstandingDal> _outstandingDalMock;
    private readonly PasienBalanceRepo _sut;

    public PasienBalanceRepoTest()
    {
        _headerDalMock = new Mock<IBilrgTataRekPasienBalanceDal>();
        _outstandingDalMock = new Mock<IBilrgTataRekPasienBalanceOutstandingDal>();
        _sut = new PasienBalanceRepo(_headerDalMock.Object, _outstandingDalMock.Object);
    }

    private static BilrgTataRekPasienBalanceDto BuildHeaderDto(int version = 0) =>
        new(
            PasienId,
            version,
            "bootstrap",
            TrsDate,
            "bootstrap",
            TrsDate,
            string.Empty,
            new DateTime(3000, 1, 1));

    [Fact]
    public void GivenNewPasienBalance_WhenSaveChanges_ThenInsertHeaderAndOutstandingRows()
    {
        var model = PasienBalanceModel.Create(PasienId);
        model.AddOutstanding("RG-001", 100_000m, 50_000m, TrsDate, "PIU001", "kasir-01");

        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IPasienKey>()))
            .Returns((BilrgTataRekPasienBalanceDto)null!);

        _sut.SaveChanges(model);

        _headerDalMock.Verify(x => x.Insert(It.Is<BilrgTataRekPasienBalanceDto>(d => d.PasienId == PasienId)), Times.Once);
        _headerDalMock.Verify(x => x.UpdateConditional(It.IsAny<BilrgTataRekPasienBalanceDto>(), It.IsAny<int>()), Times.Never);
        _outstandingDalMock.Verify(x => x.Delete(It.IsAny<IPasienKey>()), Times.Once);
        _outstandingDalMock.Verify(
            x => x.Insert(It.Is<IEnumerable<BilrgTataRekPasienBalanceOutstandingDto>>(rows => rows.Count() == 1)),
            Times.Once);
    }

    [Fact]
    public void GivenExistingPasienBalance_WhenSaveChanges_ThenUpdateConditionalAndReplaceOutstanding()
    {
        var model = PasienBalanceModel.Create(PasienId);
        model.AddOutstanding("RG-001", 100_000m, 50_000m, TrsDate, "PIU001", "kasir-01");

        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IPasienKey>()))
            .Returns(BuildHeaderDto());

        _headerDalMock
            .Setup(x => x.UpdateConditional(It.IsAny<BilrgTataRekPasienBalanceDto>(), 0))
            .Returns(1);

        _sut.SaveChanges(model);

        _headerDalMock.Verify(x => x.Insert(It.IsAny<BilrgTataRekPasienBalanceDto>()), Times.Never);
        _headerDalMock.Verify(x => x.UpdateConditional(It.IsAny<BilrgTataRekPasienBalanceDto>(), 0), Times.Once);
        _outstandingDalMock.Verify(x => x.Delete(It.IsAny<IPasienKey>()), Times.Once);
        _outstandingDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BilrgTataRekPasienBalanceOutstandingDto>>()), Times.Once);
        model.Version.Should().Be(1);
    }

    [Fact]
    public void GivenStaleVersion_WhenSaveChanges_ThenShouldThrow()
    {
        var model = PasienBalanceModel.Create(PasienId);
        model.AddOutstanding("RG-001", 100_000m, 0m, TrsDate, "PIU001", "kasir-01");

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
    public void GivenHeaderExists_WhenLoadEntity_ThenHydratesOutstandingEntries()
    {
        var header = BuildHeaderDto(version: 0);
        var outstandingDto = new BilrgTataRekPasienBalanceOutstandingDto(
            "PBO001", PasienId, "RG-001", 100_000m, 50_000m,
            TrsDate, "PIU001", TrsDate, "kasir-01");

        _headerDalMock.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(header);
        _outstandingDalMock
            .Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns([outstandingDto]);

        var result = _sut.LoadEntity(PasienBalanceModel.Key(PasienId));

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.PasienId.Should().Be(PasienId);
                model.TotalOutstandingJasa.Should().Be(100_000m);
                model.TotalOutstandingObat.Should().Be(50_000m);
                model.TotalOutstanding.Should().Be(150_000m);
                model.OutstandingEntries.Should().HaveCount(1);
                model.OutstandingEntries.Single().IsPersisted.Should().BeTrue();
            },
            onNone: () => throw new InvalidOperationException("Expected Some"));
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
