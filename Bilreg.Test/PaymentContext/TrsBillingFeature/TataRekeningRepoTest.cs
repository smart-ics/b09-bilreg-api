using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TataRekeningRepoTest
{
    private const string RegId = "RG-TATA-01";

    private readonly Mock<IBilrgTataRekeningDal> _headerDalMock;
    private readonly Mock<ITaRegistrasi3Dal> _paymentDalMock;
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock;
    private readonly TataRekeningRepo _sut;

    public TataRekeningRepoTest()
    {
        _headerDalMock = new Mock<IBilrgTataRekeningDal>();
        _paymentDalMock = new Mock<ITaRegistrasi3Dal>();
        _trsBillingRepoMock = new Mock<ITrsBillingRepo>();
        _sut = new TataRekeningRepo(
            _headerDalMock.Object,
            _paymentDalMock.Object,
            _trsBillingRepoMock.Object);
    }

    private static TataRekeningModel BuildModel(
        TataRekeningStatusEnum status = TataRekeningStatusEnum.Closed,
        IEnumerable<TataRekeningPaymentType>? payments = null) =>
        new(
            RegId,
            status,
            TataRekeningDischargeType.Default,
            payments ?? [new TataRekeningPaymentType(PaymentType.ByKas, 10_000m, 0m, CoaType.Default)],
            []);

    private static BilrgTataRekeningDto BuildHeaderDto() =>
        new(RegId, (int)TataRekeningStatusEnum.Closed, "-", new DateTime(3000, 1, 1));

    [Fact]
    public void GivenNewTataRekening_WhenSaveChanges_ThenInsertHeaderAndReplacePayments()
    {
        var model = BuildModel();
        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IRegKey>()))
            .Returns((BilrgTataRekeningDto)null!);

        _sut.SaveChanges(model);

        _headerDalMock.Verify(x => x.Insert(It.Is<BilrgTataRekeningDto>(d => d.RegId == RegId)), Times.Once);
        _headerDalMock.Verify(x => x.Update(It.IsAny<BilrgTataRekeningDto>()), Times.Never);
        _paymentDalMock.Verify(x => x.Delete(It.Is<IRegKey>(k => k.RegId == RegId)), Times.Once);
        _paymentDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TaRegistrasi3Dto>>()), Times.Once);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.IsAny<TrsBillType>()), Times.Never);
    }

    [Fact]
    public void GivenExistingTataRekening_WhenSaveChanges_ThenUpdateHeaderAndReplacePayments()
    {
        var model = BuildModel();
        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IRegKey>()))
            .Returns(BuildHeaderDto());

        _sut.SaveChanges(model);

        _headerDalMock.Verify(x => x.Update(It.Is<BilrgTataRekeningDto>(d => d.RegId == RegId)), Times.Once);
        _headerDalMock.Verify(x => x.Insert(It.IsAny<BilrgTataRekeningDto>()), Times.Never);
        _paymentDalMock.Verify(x => x.Delete(It.Is<IRegKey>(k => k.RegId == RegId)), Times.Once);
        _paymentDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TaRegistrasi3Dto>>()), Times.Once);
    }

    [Fact]
    public void GivenHeaderExists_WhenLoadEntity_ThenHydratesPaymentsAndBills()
    {
        var header = BuildHeaderDto();
        var paymentDto = new TaRegistrasi3Dto(RegId, "BYKAS", "Bayar Pribadi", 10_000m, 0m, "REK-01", false);
        var bill = TrsBillType.Key("BIL-001");

        _headerDalMock.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns(header);
        _paymentDalMock.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns([paymentDto]);
        _trsBillingRepoMock
            .Setup(x => x.ListEntity(It.IsAny<IRegKey>()))
            .Returns([]);

        var result = _sut.LoadEntity(RegModel.Key(RegId));

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.RegId.Should().Be(RegId);
                model.Status.Should().Be(TataRekeningStatusEnum.Closed);
                model.ListPayment.Should().HaveCount(1);
                model.ListPayment.First().Coa.CoaId.Should().Be("REK-01");
            },
            onNone: () => Assert.Fail("Expected Some"));
        _trsBillingRepoMock.Verify(x => x.ListEntity(It.Is<IRegKey>(k => k.RegId == RegId)), Times.Once);
    }

    [Fact]
    public void GivenHeaderNotFound_WhenLoadEntity_ThenReturnsNone()
    {
        _headerDalMock
            .Setup(x => x.GetData(It.IsAny<IRegKey>()))
            .Returns((BilrgTataRekeningDto)null!);

        var result = _sut.LoadEntity(RegModel.Key(RegId));

        result.HasValue.Should().BeFalse();
        _trsBillingRepoMock.Verify(x => x.ListEntity(It.IsAny<IRegKey>()), Times.Never);
    }

    [Fact]
    public void GivenRegKey_WhenDeleteEntity_ThenDeleteHeaderAndPaymentsOnly()
    {
        var key = RegModel.Key(RegId);

        _sut.DeleteEntity(key);

        _headerDalMock.Verify(x => x.Delete(key), Times.Once);
        _paymentDalMock.Verify(x => x.Delete(key), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
    }
}
