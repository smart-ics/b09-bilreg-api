using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class RequestTransportCmdTest
{
    private readonly Mock<ITrsTransportRepo> _trsTransportRepoMock = new();
    private readonly Mock<ITujuanTransportRepo> _tujuanTransportRepoMock = new();
    private readonly Mock<IAmbulanceRepo> _ambulanceRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly RequestTransportHandler _sut;

    public RequestTransportCmdTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 16, 10, 0, 0));
        _sut = new RequestTransportHandler(
            _trsTransportRepoMock.Object,
            _tujuanTransportRepoMock.Object,
            _ambulanceRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task GivenWilayahMethodWithExplicitKonstanta_WhenHandled_ThenIgnoresClientKonstantaAndUsesMaster()
    {
        // Arrange
        var tujuanMaster = new TujuanTransportModel("TJ-01", "RS Kariadi", 2m, false);
        _tujuanTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITujuanTransportKey>(k => k.TujuanTransportId == "TJ-01")))
            .Returns(MayBe.From(tujuanMaster));

        var cmd = new RequestTransportCmd(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER01",
            RegId: "REG-01",
            TujuanTransportId: "TJ-01",
            Konstanta: 150000m,
            LokasiAsal: "RS IGD",
            LokasiTujuan: "RS Kariadi",
            JaminanId: "BPJS",
            Keterangan: "Rujukan darurat");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Requested");
        result.TrsTransportId.Should().StartWith("TP");

        _trsTransportRepoMock.Verify(x => x.SaveChanges(It.Is<TrsTransportModel>(t =>
            t.TipePerjalanan == TransportTypeEnum.OutboundReferral &&
            t.MetodeTarif == ChargingMethodEnum.Wilayah &&
            t.TujuanTransportId == "TJ-01" &&
            t.Konstanta == 2m &&
            t.RegId == "REG-01" &&
            !t.IsNonReg &&
            t.Status == TransportStatusEnum.Requested)), Times.Once);
    }

    [Fact]
    public async Task GivenWilayahMethodWithZeroKonstanta_WhenTujuanFound_ThenResolvesKonstantaFromMaster()
    {
        // Arrange
        var tujuanMaster = new TujuanTransportModel("TJ-02", "RS Sardjito", 275000m, false);
        _tujuanTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITujuanTransportKey>(k => k.TujuanTransportId == "TJ-02")))
            .Returns(MayBe.From(tujuanMaster));

        var cmd = new RequestTransportCmd(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER01",
            TujuanTransportId: "TJ-02",
            Konstanta: 0m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Requested");
        _trsTransportRepoMock.Verify(x => x.SaveChanges(It.Is<TrsTransportModel>(t =>
            t.TujuanTransportId == "TJ-02" &&
            t.Konstanta == 275000m)), Times.Once);
    }

    [Fact]
    public async Task GivenWilayahMethodWithoutTujuanTransportId_WhenHandled_ThenThrowsArgumentException()
    {
        // Arrange
        var cmd = new RequestTransportCmd(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER01",
            TujuanTransportId: "");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Wilayah*TujuanTransportId*");
    }

    [Fact]
    public async Task GivenJarakMethod_WhenHandled_ThenThrowsArgumentException()
    {
        // Arrange
        var cmd = new RequestTransportCmd(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Jarak,
            "IGD",
            "USER01",
            TujuanTransportId: "TJ-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Jarak*");
    }

    [Fact]
    public async Task GivenAmbulanceAndTujuan_WhenHandled_ThenCalculatesAllKomponenViaCalculator()
    {
        // Arrange
        var tujuan = new TujuanTransportModel("TJ-01", "Semarang", 2m, false);
        var ambulance = new AmbulanceModel("AMB-01", "Ambulans 01");
        ambulance.SetAbonement(20000m);
        ambulance.Attach([
            new AmbulanceKomponenModel("AMB-01", "K-TETAP", 50000m, true),
            new AmbulanceKomponenModel("AMB-01", "K-VAR", 100000m, false)
        ]);
        ambulance.AddTujuan(new TransportTujuanModel("AMB-01", "TJ-01", 3m));

        _tujuanTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITujuanTransportKey>(k => k.TujuanTransportId == "TJ-01")))
            .Returns(MayBe.From(tujuan));
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-01")))
            .Returns(MayBe.From(ambulance));

        var cmd = new RequestTransportCmd(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER01",
            TujuanTransportId: "TJ-01",
            AmbulanceId: "AMB-01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Requested");
        _trsTransportRepoMock.Verify(x => x.SaveChanges(It.Is<TrsTransportModel>(t =>
            t.Konstanta == 3m &&
            t.ListItems.Count == 2 &&
            t.Total == 370000m &&
            !t.IsPostedBilling)), Times.Once);
    }

}
