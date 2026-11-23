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
    public async Task GivenWilayahMethodWithExplicitKonstanta_WhenHandled_ThenCreatesRequestedTransport()
    {
        // Arrange
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
            t.Konstanta == 150000m &&
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
    public async Task GivenJarakMethod_WhenTujuanTransportIdPassed_ThenThrowsArgumentExceptionForCoreInvariant6()
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
            .WithMessage("*Core Invariant 6*");
    }

    [Fact]
    public async Task GivenJarakMethodWithoutTujuan_WhenHandled_ThenCreatesSuccessfully()
    {
        // Arrange
        var cmd = new RequestTransportCmd(
            TransportTypeEnum.InboundRetrieval,
            ChargingMethodEnum.Jarak,
            "IGD",
            "USER01",
            LokasiAsal: "TKP Kecelakaan",
            LokasiTujuan: "RS IGD");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Requested");
        _trsTransportRepoMock.Verify(x => x.SaveChanges(It.Is<TrsTransportModel>(t =>
            t.MetodeTarif == ChargingMethodEnum.Jarak &&
            t.TujuanTransportId == string.Empty &&
            t.Konstanta == 0m &&
            t.IsNonReg)), Times.Once);
    }

    [Fact]
    public async Task GivenWilayahWithAmbulance_WhenHandled_ThenCalculatesBaseTariffAndSavesItem()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-01", "Ambulans 1", "B 1234 CD");
        ambulance.SetAbonement(100000m);
        ambulance.Add(new AmbulanceKomponenModel("KOMP-01", "Tarif Dasar", 0m, true));
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-01")))
            .Returns(MayBe.From(ambulance));

        var cmd = new RequestTransportCmd(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER01",
            RegId: "REG-01",
            TujuanTransportId: "TJ-01",
            Konstanta: 2m,
            AmbulanceId: "AMB-01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Requested");
        _trsTransportRepoMock.Verify(x => x.SaveChanges(It.Is<TrsTransportModel>(t =>
            t.Status == TransportStatusEnum.Requested &&
            !t.IsPostedBilling &&
            t.SubTotal == 200000m &&
            t.Total == 200000m &&
            t.ListItems.Count == 1)), Times.Once);
    }

    [Fact]
    public async Task GivenJarakWithAmbulanceAndKmRate_WhenHandled_ThenCalculatesBaseTariffAndSetsJarakManual()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-02", "Ambulans 2", "B 5678 EF");
        ambulance.SetAbonement(100000m);
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-02")))
            .Returns(MayBe.From(ambulance));

        var cmd = new RequestTransportCmd(
            TransportTypeEnum.InboundRetrieval,
            ChargingMethodEnum.Jarak,
            "IGD",
            "USER01",
            AmbulanceId: "AMB-02",
            KmRate: 15000m,
            TotalJarakKm: 10m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Requested");
        _trsTransportRepoMock.Verify(x => x.SaveChanges(It.Is<TrsTransportModel>(t =>
            t.Status == TransportStatusEnum.Requested &&
            !t.IsPostedBilling &&
            t.TotalJarakKm == 10m &&
            t.SubTotal == 250000m &&
            t.Total == 250000m &&
            t.ListItems.Count == 1 &&
            t.ListItems.First().DetilTarifId == "KOMP-AMB")), Times.Once);
    }
}
