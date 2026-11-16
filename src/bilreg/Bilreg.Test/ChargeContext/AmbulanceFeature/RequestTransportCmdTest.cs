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
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly RequestTransportHandler _sut;

    public RequestTransportCmdTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 16, 10, 0, 0));
        _sut = new RequestTransportHandler(
            _trsTransportRepoMock.Object,
            _tujuanTransportRepoMock.Object,
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
}
