using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeQueriesTest
{
    private readonly Mock<IRoomChargeRepo> _roomChargeRepoMock;

    public RoomChargeQueriesTest()
    {
        _roomChargeRepoMock = new Mock<IRoomChargeRepo>();
    }

    private static RoomChargeModel CreateModel(
        string id,
        string regId,
        string date,
        decimal units = 1.0m,
        decimal baseTariff = 300000m,
        decimal total = 300000m,
        bool isTitip = false,
        bool isNaik = false,
        decimal diff = 0m,
        RoomChargeStatusEnum status = RoomChargeStatusEnum.Posted)
    {
        var komponen = new List<RoomChargeKomponenModel>
        {
            RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", 200000m, 0m, "1.1.01"),
            RoomChargeKomponenModel.Create("K02", "Askep Bangsal", "Keperawatan", 100000m, 0m, "1.1.02")
        };

        return RoomChargeModel.Create(
            pakaiBedId: "PB01",
            regId: regId,
            pasienId: "PAS01",
            bangsalId: "W01",
            kamarId: "K01",
            bedId: "BED01",
            chargeCycleDate: date,
            timeCharge: new DateTime(2026, 10, 19, 12, 0, 0),
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "VIP",
            payerClassId: "KL1",
            billingClassId: "VIP",
            isTitipKelas: isTitip,
            isNaikKelas: isNaik,
            differentialAmount: diff,
            isRoomingIn: false,
            motherPakaiBedId: "",
            chargedUnits: units,
            baseTariff: baseTariff,
            discountAmount: 0m,
            totalAmount: total,
            listKomponen: komponen,
            userId: "USER01",
            roomChargeId: id);
    }

    [Fact]
    public async Task GivenExistingRoomChargeId_WhenGetQueryHandled_ThenReturnsDetailViewWithComponents()
    {
        // Arrange
        var model = CreateModel("RMC01", "REG01", "2026-10-19");
        _roomChargeRepoMock.Setup(x => x.LoadEntity("RMC01")).Returns(MayBe.From(model));

        var sut = new RoomChargeGetHandler(_roomChargeRepoMock.Object);
        var qry = new RoomChargeGetQuery("RMC01");

        // Act
        var result = await sut.Handle(qry, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RoomChargeId.Should().Be("RMC01");
        result.RegId.Should().Be("REG01");
        result.PhysicalClassId.Should().Be("VIP");
        result.ChargedUnits.Should().Be(1.0m);
        result.TotalAmount.Should().Be(300000m);
        result.ListKomponen.Should().HaveCount(2);
        result.ListKomponen.First().KomponenName.Should().Be("Sewa Kamar");
    }

    [Fact]
    public async Task GivenNonExistentRoomChargeId_WhenGetQueryHandled_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _roomChargeRepoMock.Setup(x => x.LoadEntity("NOT_EXIST")).Returns(MayBe<RoomChargeModel>.None);

        var sut = new RoomChargeGetHandler(_roomChargeRepoMock.Object);
        var qry = new RoomChargeGetQuery("NOT_EXIST");

        // Act
        var act = () => sut.Handle(qry, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenRegId_WhenListByRegQueryHandled_ThenReturnsAllMappedViews()
    {
        // Arrange
        var list = new List<RoomChargeModel>
        {
            CreateModel("RMC01", "REG01", "2026-10-18"),
            CreateModel("RMC02", "REG01", "2026-10-19")
        };
        _roomChargeRepoMock.Setup(x => x.ListDataByReg("REG01")).Returns(list);

        var sut = new RoomChargeListByRegHandler(_roomChargeRepoMock.Object);
        var qry = new RoomChargeListByRegQuery("REG01");

        // Act
        var result = (await sut.Handle(qry, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].RoomChargeId.Should().Be("RMC01");
        result[1].RoomChargeId.Should().Be("RMC02");
    }

    [Fact]
    public async Task GivenRegId_WhenSummaryByRegQueryHandled_ThenCalculatesUnitsAmountsAndSubsidiCorrectly()
    {
        // Arrange: 3 charges: 1 standard, 1 titip kelas (subsidy), 1 upgrade (differential amount)
        var c1 = CreateModel("RMC01", "REG01", "2026-10-17", units: 1.0m, baseTariff: 300000m, total: 300000m);
        var c2 = CreateModel("RMC02", "REG01", "2026-10-18", units: 1.0m, baseTariff: 500000m, total: 300000m, isTitip: true); // 200k subsidy
        var c3 = CreateModel("RMC03", "REG01", "2026-10-19", units: 1.0m, baseTariff: 500000m, total: 500000m, isNaik: true, diff: 200000m);

        var list = new List<RoomChargeModel> { c1, c2, c3 };
        _roomChargeRepoMock.Setup(x => x.ListDataByReg("REG01")).Returns(list);

        var sut = new RoomChargeSummaryByRegHandler(_roomChargeRepoMock.Object);
        var qry = new RoomChargeSummaryByRegQuery("REG01");

        // Act
        var result = await sut.Handle(qry, CancellationToken.None);

        // Assert
        result.TotalChargedUnits.Should().Be(3.0m);
        result.TotalAmount.Should().Be(1100000m);
        result.DifferentialAmount.Should().Be(200000m);
        result.TotalSubsidi.Should().Be(200000m);
        result.ActiveDaysCount.Should().Be(3);
    }
}
