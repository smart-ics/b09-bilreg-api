using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargePostDailyBatchCmdTest
{
    private readonly Mock<IRoomChargeRepo> _roomChargeRepoMock;
    private readonly Mock<IBillingLockChecker> _billingLockCheckerMock;
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock;
    private readonly Mock<ITarifRepo> _tarifRepoMock;
    private readonly Mock<IBedOccupancyQuery> _bedOccupancyQueryMock;
    private readonly Mock<IRoomChargeTarifService> _tarifServiceMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;

    private readonly RoomChargePostDailyBatchHandler _sut;

    public RoomChargePostDailyBatchCmdTest()
    {
        _roomChargeRepoMock = new Mock<IRoomChargeRepo>();
        _billingLockCheckerMock = new Mock<IBillingLockChecker>();
        _trsBillingRepoMock = new Mock<ITrsBillingRepo>();
        _tarifRepoMock = new Mock<ITarifRepo>();
        _bedOccupancyQueryMock = new Mock<IBedOccupancyQuery>();
        _tarifServiceMock = new Mock<IRoomChargeTarifService>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 19, 12, 0, 0));

        var defaultTariffResult = new RoomChargeTarifResult(300000m, new List<RoomChargeKomponenModel>
        {
            RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", 180000m, 0m, "1.1.01"),
            RoomChargeKomponenModel.Create("K02", "Askep", "Keperawatan", 75000m, 0m, "1.1.02"),
            RoomChargeKomponenModel.Create("K03", "Konsumsi", "Gizi", 45000m, 0m, "1.1.03")
        });
        _tarifServiceMock.Setup(x => x.GetTarif(It.IsAny<string>(), It.IsAny<string>())).Returns(defaultTariffResult);

        _sut = new RoomChargePostDailyBatchHandler(
            _roomChargeRepoMock.Object,
            _billingLockCheckerMock.Object,
            _trsBillingRepoMock.Object,
            _tarifRepoMock.Object,
            _bedOccupancyQueryMock.Object,
            _tarifServiceMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task GivenActiveOccupants_WhenBatchExecuted_ThenProcessesSuccessfullyAndSavesBothRoomChargeAndBilling()
    {
        // Arrange
        var occupants = new List<BedOccupantItem>
        {
            new("PB01", "REG01", "PAS01", "W01", "K01", "BED01", new DateTime(2026, 10, 18, 10, 0, 0), "VIP", "VIP", BaseTariff: 300000m, PayerTariff: 300000m)
        };
        _bedOccupancyQueryMock.Setup(x => x.GetActiveOccupants("W01", It.IsAny<DateTime>())).Returns(occupants);
        _roomChargeRepoMock.Setup(x => x.GetByPakaiBedAndDate("PB01", "2026-10-19")).Returns(MayBe<RoomChargeModel>.None);

        var cmd = new RoomChargePostDailyBatchCmd("W01", new DateTime(2026, 10, 19, 12, 0, 0), "USER01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TotalProcessed.Should().Be(1);
        result.TotalSkipped.Should().Be(0);
        result.TotalFailed.Should().Be(0);
        result.ProcessedIds.Should().HaveCount(1);

        _billingLockCheckerMock.Verify(x => x.AssertBillingNotFinal("REG01"), Times.Once);
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.PakaiBedId == "PB01" &&
            m.RoomChargeStatus == RoomChargeStatusEnum.Posted &&
            m.ChargedUnits == 1.0m &&
            m.TotalAmount == 300000m)), Times.Once);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b =>
            b.Reg.RegId == "REG01" &&
            b.Nilai.SubTotal == 300000m)), Times.Once);
    }

    [Fact]
    public async Task GivenAlreadyPostedRoomCharge_WhenBatchExecuted_ThenSkipsOccupantIdempotently()
    {
        // Arrange
        var occupants = new List<BedOccupantItem>
        {
            new("PB01", "REG01", "PAS01", "W01", "K01", "BED01", new DateTime(2026, 10, 18, 10, 0, 0), "VIP", "VIP")
        };
        _bedOccupancyQueryMock.Setup(x => x.GetActiveOccupants("W01", It.IsAny<DateTime>())).Returns(occupants);

        var existingPosted = RoomChargeModel.Create("PB01", "REG01", "PAS01", "W01", "K01", "BED01", "2026-10-19",
            new DateTime(2026, 10, 19, 12, 0, 0), ChargeTriggerTypeEnum.DailyCutOff, "VIP", "VIP", "VIP", false, false, 0m, false, "",
            1.0m, 300000m, 0m, 300000m, [], "SYSTEM");
        _roomChargeRepoMock.Setup(x => x.GetByPakaiBedAndDate("PB01", "2026-10-19")).Returns(MayBe.From(existingPosted));

        var cmd = new RoomChargePostDailyBatchCmd("W01", new DateTime(2026, 10, 19, 12, 0, 0), "USER01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TotalProcessed.Should().Be(0);
        result.TotalSkipped.Should().Be(1);
        result.TotalFailed.Should().Be(0);
        result.ProcessedIds.Should().BeEmpty();

        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.IsAny<RoomChargeModel>()), Times.Never);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.IsAny<TrsBillType>()), Times.Never);
    }

    [Fact]
    public async Task GivenBillingEpisodeFinal_WhenBatchExecuted_ThenThrowsBillingEpisodeFinalException()
    {
        // Arrange
        var occupants = new List<BedOccupantItem>
        {
            new("PB01", "REG-FINAL", "PAS01", "W01", "K01", "BED01", new DateTime(2026, 10, 18, 10, 0, 0), "VIP", "VIP")
        };
        _bedOccupancyQueryMock.Setup(x => x.GetActiveOccupants("W01", It.IsAny<DateTime>())).Returns(occupants);
        _roomChargeRepoMock.Setup(x => x.GetByPakaiBedAndDate("PB01", "2026-10-19")).Returns(MayBe<RoomChargeModel>.None);
        _billingLockCheckerMock.Setup(x => x.AssertBillingNotFinal("REG-FINAL"))
            .Throws(new BillingEpisodeFinalException("REG-FINAL"));

        var cmd = new RoomChargePostDailyBatchCmd("W01", new DateTime(2026, 10, 19, 12, 0, 0), "USER01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BillingEpisodeFinalException>()
            .WithMessage("*REG-FINAL*");
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.IsAny<RoomChargeModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenRoomingInOccupant_WhenBatchExecuted_ThenCreatesZeroChargeUnitsAndTotal()
    {
        // Arrange
        var occupants = new List<BedOccupantItem>
        {
            new("PB-BABY", "REG-BABY", "PAS-BABY", "W01", "K01", "BED01-B", new DateTime(2026, 10, 18, 10, 0, 0), "VIP", "VIP",
                IsRoomingIn: true, MotherPakaiBedId: "PB01", BaseTariff: 300000m, PayerTariff: 300000m)
        };
        _bedOccupancyQueryMock.Setup(x => x.GetActiveOccupants(null, It.IsAny<DateTime>())).Returns(occupants);
        _roomChargeRepoMock.Setup(x => x.GetByPakaiBedAndDate("PB-BABY", "2026-10-19")).Returns(MayBe<RoomChargeModel>.None);

        var cmd = new RoomChargePostDailyBatchCmd(null, new DateTime(2026, 10, 19, 12, 0, 0), "USER01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TotalProcessed.Should().Be(1);
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.PakaiBedId == "PB-BABY" &&
            m.IsRoomingIn == true &&
            m.ChargedUnits == 0.0m &&
            m.TotalAmount == 0.0m)), Times.Once);
    }

    [Fact]
    public async Task GivenPatientUpgradeClass_WhenBatchExecuted_ThenCalculatesDifferentialAmountAndSetsOnBilling()
    {
        // Arrange
        var occupants = new List<BedOccupantItem>
        {
            new("PB-UP", "REG-UP", "PAS-UP", "W01", "K01", "BED01", new DateTime(2026, 10, 18, 10, 0, 0),
                PhysicalClassId: "VVIP", PayerClassId: "KL1", IsNaikKelas: true, BaseTariff: 500000m, PayerTariff: 300000m)
        };
        _bedOccupancyQueryMock.Setup(x => x.GetActiveOccupants("W01", It.IsAny<DateTime>())).Returns(occupants);
        _roomChargeRepoMock.Setup(x => x.GetByPakaiBedAndDate("PB-UP", "2026-10-19")).Returns(MayBe<RoomChargeModel>.None);

        var vvipTariffResult = new RoomChargeTarifResult(500000m, new List<RoomChargeKomponenModel>
        {
            RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", 500000m, 0m, "1.1.01")
        });
        _tarifServiceMock.Setup(x => x.GetTarif("VVIP", It.IsAny<string>())).Returns(vvipTariffResult);

        var cmd = new RoomChargePostDailyBatchCmd("W01", new DateTime(2026, 10, 19, 12, 0, 0), "USER01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TotalProcessed.Should().Be(1);
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.IsNaikKelas == true &&
            m.DifferentialAmount == 200000m)), Times.Once);
        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b =>
            b.Nilai.Biaya == 200000m)), Times.Once);
    }
}
