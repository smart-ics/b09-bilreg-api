using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeReverseCmdTest
{
    private readonly Mock<IRoomChargeRepo> _roomChargeRepoMock;
    private readonly Mock<IBillingLockValidationService> _billingLockValidatorMock;
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly Mock<IBusinessDateStatus> _businessDateStatusMock;
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepoMock;

    private readonly RoomChargeReverseHandler _sut;

    public RoomChargeReverseCmdTest()
    {
        _roomChargeRepoMock = new Mock<IRoomChargeRepo>();
        _billingLockValidatorMock = new Mock<IBillingLockValidationService>();
        _trsBillingRepoMock = new Mock<ITrsBillingRepo>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();
        _businessDateStatusMock = new Mock<IBusinessDateStatus>();
        _tataRekeningRepoMock = new Mock<ITataRekeningRepo>();

        var defaultNow = new DateTime(2026, 10, 20, 14, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(defaultNow);
        _businessDateStatusMock.Setup(x => x.BusinessNow).Returns(defaultNow);

        _sut = new RoomChargeReverseHandler(
            _roomChargeRepoMock.Object,
            _billingLockValidatorMock.Object,
            _trsBillingRepoMock.Object,
            _tglJamProviderMock.Object,
            _businessDateStatusMock.Object,
            _tataRekeningRepoMock.Object);
    }

    private static RoomChargeModel CreatePostedCharge(string id = "RMC01")
    {
        var komponen = new List<RoomChargeKomponenModel>
        {
            RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", 200000m, 0m, "1.1.01"),
            RoomChargeKomponenModel.Create("K02", "Askep Bangsal", "Keperawatan", 100000m, 0m, "1.1.02")
        };

        return RoomChargeModel.Create(
            pakaiBedId: "PB01",
            regId: "REG01",
            pasienId: "PAS01",
            bangsalId: "W01",
            kamarId: "K01",
            bedId: "BED01",
            chargeCycleDate: "2026-10-19",
            timeCharge: new DateTime(2026, 10, 19, 12, 0, 0),
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "VIP",
            payerClassId: "VIP",
            billingClassId: "VIP",
            isTitipKelas: false,
            isNaikKelas: false,
            differentialAmount: 0m,
            isRoomingIn: false,
            motherPakaiBedId: "",
            chargedUnits: 1.0m,
            baseTariff: 300000m,
            discountAmount: 0m,
            totalAmount: 300000m,
            listKomponen: komponen,
            userId: "USER01",
            roomChargeId: id);
    }

    [Fact]
    public async Task GivenPostedRoomCharge_WhenReversed_ThenDeletesRoomChargeAndRemovesFromBilling()
    {
        // Arrange
        var source = CreatePostedCharge("RMC01");
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC01")).Returns(MayBe.From(source));

        var cmd = new RoomChargeReverseCmd("RMC01", "ADMIN01", "Salah input pasien");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ReversedRoomChargeId.Should().Be("RMC01");
        result.StornoRoomChargeId.Should().BeEmpty();

        _billingLockValidatorMock.Verify(x => x.EnsureCanModifyBillingAsync("REG01", It.IsAny<CancellationToken>()), Times.Once);

        // Room charge is deleted via repo (hard delete with void audit snapshot)
        _roomChargeRepoMock.Verify(x => x.Delete(source, "ADMIN01", new DateTime(2026, 10, 20, 14, 0, 0)), Times.Once);
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.IsAny<RoomChargeModel>()), Times.Never);

        // Billing item deleted
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k =>
            k.TrsBillingId == "RMC01")), Times.Once);
    }

    [Fact]
    public async Task GivenBusinessDateStatusInSimulation_WhenReversed_ThenUsesBusinessNowAsDeletionTimestamp()
    {
        // Arrange
        var source = CreatePostedCharge("RMC-SIM");
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC-SIM")).Returns(MayBe.From(source));

        var simulationDate = new DateTime(2026, 11, 15, 9, 30, 0);
        _businessDateStatusMock.Setup(x => x.BusinessNow).Returns(simulationDate);
        _businessDateStatusMock.Setup(x => x.IsSimulation).Returns(true);

        var cmd = new RoomChargeReverseCmd("RMC-SIM", "ADMIN01", "Simulasi pembatalan");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ReversedRoomChargeId.Should().Be("RMC-SIM");
        result.StornoRoomChargeId.Should().BeEmpty();

        _roomChargeRepoMock.Verify(x => x.Delete(source, "ADMIN01", simulationDate), Times.Once);
    }

    [Fact]
    public async Task GivenTataRekeningWithMatchingBill_WhenReversed_ThenDeletesBillFromTataRekeningAndSaves()
    {
        // Arrange
        var source = CreatePostedCharge("RMC-TRK-01");
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC-TRK-01")).Returns(MayBe.From(source));

        var trsBill = RoomChargeBillingMapper.ToTrsBill(source);
        var tataRekening = new TataRekeningModel("REG01", TataRekeningStatusEnum.Opened, TataRekeningFinalizationType.Default, [], [trsBill]);
        _tataRekeningRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG01")))
            .Returns(MayBe.From(tataRekening));

        var cmd = new RoomChargeReverseCmd("RMC-TRK-01", "ADMIN01", "Hapus dari kasir");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ReversedRoomChargeId.Should().Be("RMC-TRK-01");
        result.StornoRoomChargeId.Should().BeEmpty();

        _roomChargeRepoMock.Verify(x => x.Delete(source, "ADMIN01", It.IsAny<DateTime>()), Times.Once);
        _tataRekeningRepoMock.Verify(x => x.SaveChanges(It.Is<TataRekeningModel>(tr => !tr.ListTrsBill.Any())), Times.Once);
    }

    [Fact]
    public async Task GivenBillingIsFinal_WhenReversed_ThenThrowsBillingLockViolationException()
    {
        // Arrange
        var source = CreatePostedCharge("RMC-FINAL");
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC-FINAL")).Returns(MayBe.From(source));
        _billingLockValidatorMock.Setup(x => x.EnsureCanModifyBillingAsync("REG01", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BillingLockViolationException("REG01", TataRekeningStatusEnum.Finalized));

        var cmd = new RoomChargeReverseCmd("RMC-FINAL", "ADMIN01", "Koreksi");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BillingLockViolationException>();
        _roomChargeRepoMock.Verify(x => x.Delete(It.IsAny<IRoomChargeKey>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.IsAny<RoomChargeModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenAlreadyReversedRoomCharge_WhenReversed_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var source = CreatePostedCharge("RMC-ALREADY");
        source.Reverse("USER01", "Previously reversed", new DateTime(2026, 10, 19));
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC-ALREADY")).Returns(MayBe.From(source));

        var cmd = new RoomChargeReverseCmd("RMC-ALREADY", "ADMIN01", "Second reversal");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot reverse room charge*");
        _roomChargeRepoMock.Verify(x => x.Delete(It.IsAny<IRoomChargeKey>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GivenAdjustedRoomCharge_WhenReversed_ThenSuccessfullyDeletesRoomChargeAndRemovesBilling()
    {
        // Arrange
        var source = CreatePostedCharge("RMC-ADJ-01");
        source.Adjust(1.0m, 350000m, 0m, 350000m, [], "USER01", new DateTime(2026, 10, 20, 10, 0, 0));
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC-ADJ-01")).Returns(MayBe.From(source));

        var cmd = new RoomChargeReverseCmd("RMC-ADJ-01", "ADMIN01", "Salah hitung kamar adjusted");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ReversedRoomChargeId.Should().Be("RMC-ADJ-01");
        result.StornoRoomChargeId.Should().BeEmpty();
        _roomChargeRepoMock.Verify(x => x.Delete(source, "ADMIN01", new DateTime(2026, 10, 20, 14, 0, 0)), Times.Once);
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == "RMC-ADJ-01")), Times.Once);
    }

    [Fact]
    public async Task GivenLongBillingRefIdExceeding15Chars_WhenReversed_ThenDeletesBothTruncatedAndRawBillingId()
    {
        // Arrange
        var source = CreatePostedCharge("RMC-LONG-01");
        source.SetBillingItemRefId("RMC-VERY-LONG-IDENTIFIER-12345");
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC-LONG-01")).Returns(MayBe.From(source));

        var cmd = new RoomChargeReverseCmd("RMC-LONG-01", "ADMIN01", "Hapus ID panjang");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ReversedRoomChargeId.Should().Be("RMC-LONG-01");
        result.StornoRoomChargeId.Should().BeEmpty();

        // 15 chars truncated
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == "RMC-VERY-LONG-I")), Times.Once);
        // Raw id
        _trsBillingRepoMock.Verify(x => x.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == "RMC-VERY-LONG-IDENTIFIER-12345")), Times.Once);
    }

    [Fact]
    public async Task GivenNonExistentRoomCharge_WhenReversed_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _roomChargeRepoMock.Setup(x => x.LoadEntity("NOT_EXIST")).Returns(MayBe<RoomChargeModel>.None);

        var cmd = new RoomChargeReverseCmd("NOT_EXIST", "ADMIN01", "Reason");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _roomChargeRepoMock.Verify(x => x.Delete(It.IsAny<IRoomChargeKey>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }
}
