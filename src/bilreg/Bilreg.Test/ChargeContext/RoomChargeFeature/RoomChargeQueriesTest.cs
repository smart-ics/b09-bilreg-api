using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeQueriesTest
{
    private readonly Mock<IRoomChargeRepo> _roomChargeRepoMock;
    private readonly Mock<IRegRepo> _regRepoMock;
    private readonly Mock<IBusinessDateStatus> _businessDateStatusMock;

    public RoomChargeQueriesTest()
    {
        _roomChargeRepoMock = new Mock<IRoomChargeRepo>();
        _regRepoMock = new Mock<IRegRepo>();
        _businessDateStatusMock = new Mock<IBusinessDateStatus>();
    }

    private void SetupReg(string regId, DateOnly admissionDate)
    {
        var reg = new RegModel(
            regId,
            admissionDate,
            new AuditInfoType("USER01", admissionDate.ToString("yyyy-MM-dd"), "10:00:00"),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            new KelasReff("KL1", "Kelas 1"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(reg));
    }

    private void SetupBusinessDate(DateTime businessNow)
    {
        _businessDateStatusMock.Setup(x => x.BusinessNow).Returns(businessNow);
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

        var model = RoomChargeModel.Create(
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

        if (status == RoomChargeStatusEnum.Reversed)
        {
            model.Reverse("USER01", "Test reversal", DateTime.Now);
        }

        return model;
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

    [Fact]
    public async Task GivenAdmissionDateAndBusinessDate_WhenListByReg_ThenExcludesTransactionsBeforeAdmissionDate()
    {
        // Arrange
        const string regId = "REG01";
        SetupReg(regId, new DateOnly(2026, 10, 18));
        SetupBusinessDate(new DateTime(2026, 10, 25));

        var list = new List<RoomChargeModel>
        {
            CreateModel("RMC01", regId, "2026-10-17"), // Before admission
            CreateModel("RMC02", regId, "2026-10-18"), // On admission
            CreateModel("RMC03", regId, "2026-10-19")  // Within range
        };
        _roomChargeRepoMock.Setup(x => x.ListDataByReg(regId)).Returns(list);

        var sut = new RoomChargeListByRegHandler(
            _roomChargeRepoMock.Object,
            _regRepoMock.Object,
            _businessDateStatusMock.Object);
        var qry = new RoomChargeListByRegQuery(regId);

        // Act
        var result = (await sut.Handle(qry, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.RoomChargeId).Should().Equal(new[] { "RMC02", "RMC03" });
    }

    [Fact]
    public async Task GivenAdmissionDateAndBusinessDate_WhenListByReg_ThenExcludesTransactionsAfterBusinessDate()
    {
        // Arrange
        const string regId = "REG01";
        SetupReg(regId, new DateOnly(2026, 10, 15));
        SetupBusinessDate(new DateTime(2026, 10, 19));

        var list = new List<RoomChargeModel>
        {
            CreateModel("RMC01", regId, "2026-10-18"), // Within range
            CreateModel("RMC02", regId, "2026-10-19"), // On business date
            CreateModel("RMC03", regId, "2026-10-20")  // After business date
        };
        _roomChargeRepoMock.Setup(x => x.ListDataByReg(regId)).Returns(list);

        var sut = new RoomChargeListByRegHandler(
            _roomChargeRepoMock.Object,
            _regRepoMock.Object,
            _businessDateStatusMock.Object);
        var qry = new RoomChargeListByRegQuery(regId);

        // Act
        var result = (await sut.Handle(qry, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.RoomChargeId).Should().Equal(new[] { "RMC01", "RMC02" });
    }

    [Fact]
    public async Task GivenAdmissionDateAndBusinessDate_WhenListByReg_ThenIncludesValidTransactionsWithinRange()
    {
        // Arrange
        const string regId = "REG01";
        SetupReg(regId, new DateOnly(2026, 10, 17));
        SetupBusinessDate(new DateTime(2026, 10, 20));

        var list = new List<RoomChargeModel>
        {
            CreateModel("RMC00", regId, "2026-10-16"), // Before admission
            CreateModel("RMC01", regId, "2026-10-17"), // Admission boundary
            CreateModel("RMC02", regId, "2026-10-18"), // Middle
            CreateModel("RMC03", regId, "2026-10-20"), // Business date boundary
            CreateModel("RMC04", regId, "2026-10-21")  // After business date
        };
        _roomChargeRepoMock.Setup(x => x.ListDataByReg(regId)).Returns(list);

        var sut = new RoomChargeListByRegHandler(
            _roomChargeRepoMock.Object,
            _regRepoMock.Object,
            _businessDateStatusMock.Object);
        var qry = new RoomChargeListByRegQuery(regId);

        // Act
        var result = (await sut.Handle(qry, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Select(x => x.RoomChargeId).Should().Equal(new[] { "RMC01", "RMC02", "RMC03" });
    }

    [Fact]
    public async Task GivenAdmissionDateAheadOfBusinessDate_WhenListByReg_ThenFallsBackSafelyAndReturnsChargesUpToBusinessDate()
    {
        // Arrange: Anomaly/simulation case where RegDate (e.g. 2026-02-11) is ahead of BusinessDate (e.g. 2024-06-06)
        const string regId = "REG01";
        SetupReg(regId, new DateOnly(2026, 2, 11));
        SetupBusinessDate(new DateTime(2024, 6, 6));

        var list = new List<RoomChargeModel>
        {
            CreateModel("RMC01", regId, "2024-06-06"), // On business date
            CreateModel("RMC02", regId, "2024-06-07")  // After business date
        };
        _roomChargeRepoMock.Setup(x => x.ListDataByReg(regId)).Returns(list);

        var sut = new RoomChargeListByRegHandler(
            _roomChargeRepoMock.Object,
            _regRepoMock.Object,
            _businessDateStatusMock.Object);
        var qry = new RoomChargeListByRegQuery(regId);

        // Act
        var result = (await sut.Handle(qry, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].RoomChargeId.Should().Be("RMC01");
    }

    [Fact]
    public async Task GivenTransactionsWithReversedAndOutsideDateRange_WhenSummaryByReg_ThenCalculatesCleanTotalsOnlyForActiveRecordsWithinRange()
    {
        // Arrange
        const string regId = "REG01";
        SetupReg(regId, new DateOnly(2026, 10, 17));
        SetupBusinessDate(new DateTime(2026, 10, 19));

        var cBefore = CreateModel("RMC00", regId, "2026-10-16", units: 1.0m, baseTariff: 300000m, total: 300000m);
        var c1 = CreateModel("RMC01", regId, "2026-10-17", units: 1.0m, baseTariff: 300000m, total: 300000m);
        var c2 = CreateModel("RMC02", regId, "2026-10-18", units: 1.0m, baseTariff: 500000m, total: 300000m, isTitip: true); // 200k subsidy
        var cReversed = CreateModel("RMC02_REV", regId, "2026-10-18", units: 1.0m, baseTariff: 500000m, total: 300000m, status: RoomChargeStatusEnum.Reversed);
        var c3 = CreateModel("RMC03", regId, "2026-10-19", units: 1.0m, baseTariff: 500000m, total: 500000m, isNaik: true, diff: 200000m);
        var cAfter = CreateModel("RMC04", regId, "2026-10-20", units: 1.0m, baseTariff: 300000m, total: 300000m);

        var list = new List<RoomChargeModel> { cBefore, c1, c2, cReversed, c3, cAfter };
        _roomChargeRepoMock.Setup(x => x.ListDataByReg(regId)).Returns(list);

        var sut = new RoomChargeSummaryByRegHandler(
            _roomChargeRepoMock.Object,
            _regRepoMock.Object,
            _businessDateStatusMock.Object);
        var qry = new RoomChargeSummaryByRegQuery(regId);

        // Act
        var result = await sut.Handle(qry, CancellationToken.None);

        // Assert: only c1, c2, c3 are active within [2026-10-17, 2026-10-19]
        result.TotalChargedUnits.Should().Be(3.0m);
        result.TotalAmount.Should().Be(1100000m);
        result.DifferentialAmount.Should().Be(200000m);
        result.TotalSubsidi.Should().Be(200000m);
        result.ActiveDaysCount.Should().Be(3);
    }

    [Fact]
    public async Task GivenRoomChargesAcrossTransferredBeds_WhenListByReg_ThenPreservesDistinctClassNamesForEachStay()
    {
        // Arrange
        const string regId = "REG_TRANSFER";
        SetupReg(regId, new DateOnly(2024, 6, 5));
        SetupBusinessDate(new DateTime(2024, 6, 7));

        // Stay 1: Kelas III on 2024-06-05
        var stay1 = new RoomChargeModel(
            "RMC01", "PB01", regId, "PAS01", "W01", "K01", "BED01",
            "2024-06-05", new DateTime(2024, 6, 5, 12, 0, 0), ChargeTriggerTypeEnum.DailyCutOff,
            "401", "401", "401", false, false, 0m, false, "",
            1.0m, 150000m, 0m, 150000m, RoomChargeStatusEnum.Posted,
            "", "", "", AuditTrailType.Default, null,
            bedCode: "BED-01", kamarName: "Kamar Mawar 1", bangsalName: "Bangsal Mawar",
            physicalClassName: "III", payerClassName: "III", billingClassName: "III");

        // Stay 2: Transferred to Kelas II on 2024-06-06
        var stay2 = new RoomChargeModel(
            "RMC02", "PB02", regId, "PAS01", "W02", "K02", "BED02",
            "2024-06-06", new DateTime(2024, 6, 6, 12, 0, 0), ChargeTriggerTypeEnum.DailyCutOff,
            "302", "401", "302", false, true, 150000m, false, "",
            1.0m, 300000m, 0m, 300000m, RoomChargeStatusEnum.Posted,
            "", "", "", AuditTrailType.Default, null,
            bedCode: "BED-02", kamarName: "Kamar Melati 2", bangsalName: "Bangsal Melati",
            physicalClassName: "II", payerClassName: "III", billingClassName: "II");

        _roomChargeRepoMock.Setup(x => x.ListDataByReg(regId)).Returns([stay1, stay2]);

        var sut = new RoomChargeListByRegHandler(
            _roomChargeRepoMock.Object,
            _regRepoMock.Object,
            _businessDateStatusMock.Object);
        var qry = new RoomChargeListByRegQuery(regId);

        // Act
        var result = (await sut.Handle(qry, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].BillingClassName.Should().Be("III");
        result[0].BedCode.Should().Be("BED-01");
        result[0].KamarName.Should().Be("Kamar Mawar 1");

        result[1].BillingClassName.Should().Be("II");
        result[1].BedCode.Should().Be("BED-02");
        result[1].KamarName.Should().Be("Kamar Melati 2");
    }
}
