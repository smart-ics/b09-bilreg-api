using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeReconcileCheckoutCmdTest
{
    private readonly Mock<IRoomChargeRepo> _roomChargeRepoMock;
    private readonly Mock<IBillingLockChecker> _billingLockCheckerMock;
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock;
    private readonly Mock<IBedOccupancyQuery> _bedOccupancyQueryMock;
    private readonly Mock<IRoomChargeTarifService> _tarifServiceMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;

    private readonly RoomChargeReconcileCheckoutHandler _sut;

    public RoomChargeReconcileCheckoutCmdTest()
    {
        _roomChargeRepoMock = new Mock<IRoomChargeRepo>();
        _billingLockCheckerMock = new Mock<IBillingLockChecker>();
        _trsBillingRepoMock = new Mock<ITrsBillingRepo>();
        _bedOccupancyQueryMock = new Mock<IBedOccupancyQuery>();
        _tarifServiceMock = new Mock<IRoomChargeTarifService>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 16, 0, 0));

        var defaultTariffResult = new RoomChargeTarifResult(400000m, new List<RoomChargeKomponenModel>
        {
            RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", 240000m, 0m, "1.1.01"),
            RoomChargeKomponenModel.Create("K02", "Askep", "Keperawatan", 100000m, 0m, "1.1.02"),
            RoomChargeKomponenModel.Create("K03", "Konsumsi", "Gizi", 60000m, 0m, "1.1.03")
        });
        _tarifServiceMock.Setup(x => x.GetTarif(It.IsAny<string>(), It.IsAny<string>())).Returns(defaultTariffResult);

        _sut = new RoomChargeReconcileCheckoutHandler(
            _roomChargeRepoMock.Object,
            _billingLockCheckerMock.Object,
            _trsBillingRepoMock.Object,
            _bedOccupancyQueryMock.Object,
            _tarifServiceMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task GivenCheckoutDelayWithinTwoHours_WhenReconciled_ThenReturnsZeroUnitsAndDoesNotSave()
    {
        // Arrange: Checkin on 2026-10-18, Checkout on 2026-10-20 at 13:30 (delay 1.5h <= 2h)
        var occupant = new BedOccupantItem("PB01", "REG01", "PAS01", "W01", "K01", "BED01",
            new DateTime(2026, 10, 18, 10, 0, 0), "VIP", "VIP", BaseTariff: 400000m, PayerTariff: 400000m);
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB01")).Returns(occupant);

        var checkoutTime = new DateTime(2026, 10, 20, 13, 30, 0);
        var cmd = new RoomChargeReconcileCheckoutCmd("PB01", checkoutTime, "NURSE01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ChargedUnits.Should().Be(0m);
        result.TotalAmount.Should().Be(0m);
        result.RoomChargeId.Should().BeNull();

        _billingLockCheckerMock.Verify(x => x.AssertBillingNotFinal("REG01"), Times.Once);
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.IsAny<RoomChargeModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.IsAny<TrsBillType>()), Times.Never);
    }

    [Fact]
    public async Task GivenCheckoutDelayBetweenTwoAndSixHours_WhenReconciled_ThenChargesHalfDayUnits()
    {
        // Arrange: Checkin on 2026-10-18, Checkout on 2026-10-20 at 16:00 (delay 4h: 2-6h)
        var occupant = new BedOccupantItem("PB01", "REG01", "PAS01", "W01", "K01", "BED01",
            new DateTime(2026, 10, 18, 10, 0, 0), "VIP", "VIP", BaseTariff: 400000m, PayerTariff: 400000m);
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB01")).Returns(occupant);

        var checkoutTime = new DateTime(2026, 10, 20, 16, 0, 0);
        var cmd = new RoomChargeReconcileCheckoutCmd("PB01", checkoutTime, "NURSE01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ChargedUnits.Should().Be(0.5m);
        result.TotalAmount.Should().Be(200000m); // 400000 * 0.5
        result.RoomChargeId.Should().NotBeNullOrEmpty();

        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.PakaiBedId == "PB01" &&
            m.ChargeTriggerType == ChargeTriggerTypeEnum.CheckoutReconciliation &&
            m.ChargedUnits == 0.5m &&
            m.TotalAmount == 200000m)), Times.Once);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b =>
            b.Nilai.SubTotal == 200000m)), Times.Once);
    }

    [Fact]
    public async Task GivenSameDayStay_WhenReconciled_ThenChargesMinimumOneFullDay()
    {
        // Arrange: Checkin on 2026-10-20 at 08:00, Checkout on 2026-10-20 at 11:30 (same date)
        var occupant = new BedOccupantItem("PB-SAMEDAY", "REG-SAME", "PAS01", "W01", "K01", "BED01",
            new DateTime(2026, 10, 20, 8, 0, 0), "VIP", "VIP", BaseTariff: 400000m, PayerTariff: 400000m);
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB-SAMEDAY")).Returns(occupant);

        var checkoutTime = new DateTime(2026, 10, 20, 11, 30, 0);
        var cmd = new RoomChargeReconcileCheckoutCmd("PB-SAMEDAY", checkoutTime, "NURSE01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ChargedUnits.Should().Be(1.0m);
        result.TotalAmount.Should().Be(400000m);

        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.ChargedUnits == 1.0m &&
            m.TotalAmount == 400000m)), Times.Once);
    }

    [Fact]
    public async Task GivenBillingIsFinal_WhenReconciled_ThenThrowsBillingEpisodeFinalException()
    {
        // Arrange
        var occupant = new BedOccupantItem("PB01", "REG-FINAL", "PAS01", "W01", "K01", "BED01",
            new DateTime(2026, 10, 18, 10, 0, 0), "VIP", "VIP", BaseTariff: 400000m);
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB01")).Returns(occupant);
        _billingLockCheckerMock.Setup(x => x.AssertBillingNotFinal("REG-FINAL"))
            .Throws(new BillingEpisodeFinalException("REG-FINAL"));

        var cmd = new RoomChargeReconcileCheckoutCmd("PB01", new DateTime(2026, 10, 20, 16, 0, 0), "NURSE01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BillingEpisodeFinalException>();
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.IsAny<RoomChargeModel>()), Times.Never);
    }
}
