using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
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
    private readonly Mock<IBillingLockChecker> _billingLockCheckerMock;
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;

    private readonly RoomChargeReverseHandler _sut;

    public RoomChargeReverseCmdTest()
    {
        _roomChargeRepoMock = new Mock<IRoomChargeRepo>();
        _billingLockCheckerMock = new Mock<IBillingLockChecker>();
        _trsBillingRepoMock = new Mock<ITrsBillingRepo>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 14, 0, 0));

        _sut = new RoomChargeReverseHandler(
            _roomChargeRepoMock.Object,
            _billingLockCheckerMock.Object,
            _trsBillingRepoMock.Object,
            _tglJamProviderMock.Object);
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
    public async Task GivenPostedRoomCharge_WhenReversed_ThenUpdatesSourceToReversedAndCreatesStornoRecord()
    {
        // Arrange
        var source = CreatePostedCharge("RMC01");
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC01")).Returns(MayBe.From(source));

        var cmd = new RoomChargeReverseCmd("RMC01", "ADMIN01", "Salah input pasien");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ReversedRoomChargeId.Should().Be("RMC01");
        result.StornoRoomChargeId.Should().NotBeNullOrEmpty();
        result.StornoRoomChargeId.Should().NotBe("RMC01");

        _billingLockCheckerMock.Verify(x => x.AssertBillingNotFinal("REG01"), Times.Once);

        // Source saved with status Reversed
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.RoomChargeId == "RMC01" &&
            m.RoomChargeStatus == RoomChargeStatusEnum.Reversed &&
            m.ReversalReason == "Salah input pasien")), Times.Once);

        // Storno record saved with negative values and reference to RMC01
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.RoomChargeId == result.StornoRoomChargeId &&
            m.ReversalOfRoomChargeId == "RMC01" &&
            m.ChargedUnits == -1.0m &&
            m.TotalAmount == -300000m)), Times.Once);

        // Billing item saved
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b =>
            b.Reg.RegId == "REG01" &&
            b.Nilai.SubTotal == -300000m)), Times.Once);
    }

    [Fact]
    public async Task GivenBillingIsFinal_WhenReversed_ThenThrowsBillingEpisodeFinalException()
    {
        // Arrange
        var source = CreatePostedCharge("RMC-FINAL");
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC-FINAL")).Returns(MayBe.From(source));
        _billingLockCheckerMock.Setup(x => x.AssertBillingNotFinal("REG01"))
            .Throws(new BillingEpisodeFinalException("REG01"));

        var cmd = new RoomChargeReverseCmd("RMC-FINAL", "ADMIN01", "Koreksi");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BillingEpisodeFinalException>();
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
    }
}
