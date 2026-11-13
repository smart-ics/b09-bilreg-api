using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargePostManualCmdTest
{
    private readonly Mock<IRoomChargeRepo> _roomChargeRepoMock;
    private readonly Mock<IBillingLockChecker> _billingLockCheckerMock;
    private readonly Mock<ITrsBillingRepo> _trsBillingRepoMock;
    private readonly Mock<IBedOccupancyQuery> _bedOccupancyQueryMock;
    private readonly Mock<IRoomChargeTarifService> _tarifServiceMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;

    private readonly RoomChargePostManualHandler _sut;

    public RoomChargePostManualCmdTest()
    {
        _roomChargeRepoMock = new Mock<IRoomChargeRepo>();
        _billingLockCheckerMock = new Mock<IBillingLockChecker>();
        _trsBillingRepoMock = new Mock<ITrsBillingRepo>();
        _bedOccupancyQueryMock = new Mock<IBedOccupancyQuery>();
        _tarifServiceMock = new Mock<IRoomChargeTarifService>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 28, 12, 0, 0));

        var defaultTariffResult = new RoomChargeTarifResult(250000m, new List<RoomChargeKomponenModel>
        {
            RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", 150000m, 0m, "1.1.01"),
            RoomChargeKomponenModel.Create("K02", "Askep", "Keperawatan", 60000m, 0m, "1.1.02"),
            RoomChargeKomponenModel.Create("K03", "Konsumsi", "Gizi", 40000m, 0m, "1.1.03")
        });
        _tarifServiceMock.Setup(x => x.GetTarif(It.IsAny<string>(), It.IsAny<string>())).Returns(defaultTariffResult);

        _sut = new RoomChargePostManualHandler(
            _roomChargeRepoMock.Object,
            _billingLockCheckerMock.Object,
            _trsBillingRepoMock.Object,
            _bedOccupancyQueryMock.Object,
            _tarifServiceMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task GivenValidPakaiBedId_WhenManualChargeExecuted_ThenSavesBothRoomChargeAndTrsBill()
    {
        // Arrange
        var occupant = new BedOccupantItem(
            "PB001", "REG001", "PAS001", "W01", "K01", "BED01",
            new DateTime(2026, 10, 27, 10, 0, 0),
            "Kelas I", "Kelas I", BaseTariff: 250000m, PayerTariff: 250000m
        );
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB001")).Returns(occupant);

        var cmd = new RoomChargePostManualCmd(
            PakaiBedId: "PB001",
            RegId: "REG001",
            ChargeDate: new DateTime(2026, 10, 28),
            UserId: "USER01",
            Notes: "Penyesuaian sewa kamar manual"
        );

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().Be("PB001");
        result.RegId.Should().Be("REG001");
        result.ChargeTriggerType.Should().Be("ManualAdjustment");
        result.TotalAmount.Should().Be(250000m);

        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.PakaiBedId == "PB001" &&
            m.ChargeTriggerType == ChargeTriggerTypeEnum.ManualAdjustment &&
            m.TotalAmount == 250000m
        )), Times.Once);

        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.IsAny<TrsBillType>()), Times.Once);
    }

    [Fact]
    public async Task GivenCustomRate_WhenManualChargeExecuted_ThenAppliesCustomRate()
    {
        // Arrange
        var occupant = new BedOccupantItem(
            "PB002", "REG002", "PAS002", "W01", "K01", "BED02",
            new DateTime(2026, 10, 27, 10, 0, 0),
            "Kelas II", "Kelas II", BaseTariff: 150000m, PayerTariff: 150000m
        );
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB002")).Returns(occupant);

        var cmd = new RoomChargePostManualCmd(
            PakaiBedId: "PB002",
            RegId: "REG002",
            ChargeDate: new DateTime(2026, 10, 28),
            UserId: "USER01",
            Rate: 200000m
        );

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(200000m);
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m => m.TotalAmount == 200000m)), Times.Once);
    }

    [Fact]
    public async Task GivenExistingActiveChargeOnSameDate_WhenManualChargeExecuted_ThenUpdatesExistingChargeAndSyncsBilling()
    {
        // Arrange
        var occupant = new BedOccupantItem(
            "PB003", "REG003", "PAS003", "W01", "K01", "BED03",
            new DateTime(2026, 10, 27, 10, 0, 0),
            "Kelas I", "Kelas I", BaseTariff: 250000m, PayerTariff: 250000m
        );
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB003")).Returns(occupant);

        var existingCharge = RoomChargeModel.Create(
            "PB003", "REG003", "PAS003", "W01", "K01", "BED03",
            "2026-10-28", new DateTime(2026, 10, 28, 12, 0, 0), ChargeTriggerTypeEnum.DailyCutOff,
            "Kelas I", "Kelas I", "Kelas I", false, false, 0m, false, "",
            1.0m, 250000m, 0m, [], "SYSTEM", roomChargeId: "RMC-EXIST-01");

        _roomChargeRepoMock.Setup(x => x.GetByPakaiBedAndDate("PB003", "2026-10-28"))
            .Returns(Nuna.Lib.PatternHelper.MayBe.From(existingCharge));

        var cmd = new RoomChargePostManualCmd(
            PakaiBedId: "PB003",
            RegId: "REG003",
            ChargeDate: new DateTime(2026, 10, 28, 14, 30, 0),
            UserId: "USER01",
            Rate: 350000m
        );

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RoomChargeId.Should().Be("RMC-EXIST-01");
        result.TotalAmount.Should().Be(350000m);
        result.RoomChargeStatus.Should().Be("Adjusted");

        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.RoomChargeId == "RMC-EXIST-01" &&
            m.TotalAmount == 350000m &&
            m.RoomChargeStatus == RoomChargeStatusEnum.Adjusted
        )), Times.Once);

        _trsBillingRepoMock.Verify(x => x.SaveChanges(It.Is<TrsBillType>(b =>
            b.TrsBillingId == "RMC-EXIST-01" &&
            b.Nilai.SubTotal == 350000m
        )), Times.Once);
    }

    [Fact]
    public async Task GivenReversedChargeOnSameDate_WhenManualChargeExecuted_ThenAllowsNewManualCharge()
    {
        // Arrange
        var occupant = new BedOccupantItem(
            "PB004", "REG004", "PAS004", "W01", "K01", "BED04",
            new DateTime(2026, 10, 27, 10, 0, 0),
            "Kelas I", "Kelas I", BaseTariff: 250000m, PayerTariff: 250000m
        );
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB004")).Returns(occupant);

        var reversedCharge = RoomChargeModel.Create(
            "PB004", "REG004", "PAS004", "W01", "K01", "BED04",
            "2026-10-28", new DateTime(2026, 10, 28, 12, 0, 0), ChargeTriggerTypeEnum.DailyCutOff,
            "Kelas I", "Kelas I", "Kelas I", false, false, 0m, false, "",
            1.0m, 250000m, 0m, [], "SYSTEM");
        reversedCharge.Reverse("USER01", "Salah input", DateTime.Now);

        _roomChargeRepoMock.Setup(x => x.GetByPakaiBedAndDate("PB004", "2026-10-28"))
            .Returns(Nuna.Lib.PatternHelper.MayBe.From(reversedCharge));

        var cmd = new RoomChargePostManualCmd(
            PakaiBedId: "PB004",
            RegId: "REG004",
            ChargeDate: new DateTime(2026, 10, 28),
            UserId: "USER01"
        );

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _roomChargeRepoMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m => m.PakaiBedId == "PB004")), Times.Once);
    }

    [Fact]
    public async Task GivenChargeDateExceedsBusinessDate_WhenManualChargeExecuted_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var occupant = new BedOccupantItem(
            "PB005", "REG005", "PAS005", "W01", "K01", "BED05",
            new DateTime(2026, 10, 27, 10, 0, 0),
            "Kelas I", "Kelas I", BaseTariff: 250000m, PayerTariff: 250000m
        );
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB005")).Returns(occupant);

        // Business date is 2026-10-28, user attempts 2026-10-29
        var cmd = new RoomChargePostManualCmd(
            PakaiBedId: "PB005",
            RegId: "REG005",
            ChargeDate: new DateTime(2026, 10, 29),
            UserId: "USER01"
        );

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak boleh melebihi business date*");
    }

    [Fact]
    public async Task GivenChargeDateOnOrBeforeBusinessDate_WhenManualChargeExecuted_ThenSucceeds()
    {
        // Arrange
        var occupant = new BedOccupantItem(
            "PB006", "REG006", "PAS006", "W01", "K01", "BED06",
            new DateTime(2026, 10, 27, 10, 0, 0),
            "Kelas I", "Kelas I", BaseTariff: 250000m, PayerTariff: 250000m
        );
        _bedOccupancyQueryMock.Setup(x => x.GetOccupant("PB006")).Returns(occupant);

        // Business date is 2026-10-28, user charges for past date 2026-10-27
        var cmd = new RoomChargePostManualCmd(
            PakaiBedId: "PB006",
            RegId: "REG006",
            ChargeDate: new DateTime(2026, 10, 27),
            UserId: "USER01"
        );

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ChargeCycleDate.Should().Be("2026-10-27");
    }
}
