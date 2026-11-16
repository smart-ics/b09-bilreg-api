using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class DispatchTransportCmdTest
{
    private readonly Mock<ITrsTransportRepo> _trsTransportRepoMock = new();
    private readonly Mock<IAmbulanceRepo> _ambulanceRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly DispatchTransportHandler _sut;

    public DispatchTransportCmdTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 16, 10, 30, 0));
        _sut = new DispatchTransportHandler(
            _trsTransportRepoMock.Object,
            _ambulanceRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static TrsTransportModel CreateRequestedTransport(string id = "TP001")
    {
        return TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER1",
            id,
            DateTime.Now,
            tujuanTransportId: "TJ-01",
            konstanta: 100000m);
    }

    [Fact]
    public async Task GivenValidRequest_WhenDispatched_ThenUpdatesStatusAndVehicleDetails()
    {
        // Arrange
        var transport = CreateRequestedTransport("TP001");
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance 1", "B 1234 CD", "ALS", "Available");

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP001")))
            .Returns(MayBe.From(transport));

        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-01")))
            .Returns(MayBe.From(ambulance));

        _trsTransportRepoMock.Setup(x => x.HasActiveDispatchForAmbulance("AMB-01", "TP001")).Returns(false);
        _trsTransportRepoMock.Setup(x => x.HasActiveDispatchForDriver("DRV-01", "TP001")).Returns(false);

        var cmd = new DispatchTransportCmd(
            "TP001",
            "AMB-01",
            "DRV-01",
            12500.5m,
            PerawatId: "PRW-01",
            DokterId: "DR-01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Dispatched");
        result.TrsTransportId.Should().Be("TP001");
        result.DispatchTime.Should().Be(new DateTime(2026, 11, 16, 10, 30, 0));

        transport.Status.Should().Be(TransportStatusEnum.Dispatched);
        transport.AmbulanceId.Should().Be("AMB-01");
        transport.DriverId.Should().Be("DRV-01");
        transport.NoPolisi.Should().Be("B 1234 CD"); // Inherited from ambulance
        transport.StartOdometer.Should().Be(12500.5m);
        transport.PerawatId.Should().Be("PRW-01");
        transport.DokterId.Should().Be("DR-01");

        _trsTransportRepoMock.Verify(x => x.SaveChanges(transport), Times.Once);
    }

    [Fact]
    public async Task GivenAmbulanceAlreadyActive_WhenDispatched_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var transport = CreateRequestedTransport("TP002");
        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP002")))
            .Returns(MayBe.From(transport));

        _trsTransportRepoMock.Setup(x => x.HasActiveDispatchForAmbulance("AMB-01", "TP002")).Returns(true);

        var cmd = new DispatchTransportCmd("TP002", "AMB-01", "DRV-01", 1000m);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ambulans 'AMB-01' sedang bertugas*");
    }

    [Fact]
    public async Task GivenDriverAlreadyActive_WhenDispatched_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var transport = CreateRequestedTransport("TP003");
        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP003")))
            .Returns(MayBe.From(transport));

        _trsTransportRepoMock.Setup(x => x.HasActiveDispatchForAmbulance("AMB-01", "TP003")).Returns(false);
        _trsTransportRepoMock.Setup(x => x.HasActiveDispatchForDriver("DRV-01", "TP003")).Returns(true);

        var cmd = new DispatchTransportCmd("TP003", "AMB-01", "DRV-01", 1000m);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pengemudi 'DRV-01' sedang bertugas*");
    }

    [Fact]
    public async Task GivenAmbulanceInMaintenance_WhenDispatched_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var transport = CreateRequestedTransport("TP004");
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance 1", statusArmada: "Maintenance");

        _trsTransportRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITrsTransportKey>(k => k.TrsTransportId == "TP004")))
            .Returns(MayBe.From(transport));

        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.Is<IAmbulanceKey>(k => k.AmbulanceId == "AMB-01")))
            .Returns(MayBe.From(ambulance));

        _trsTransportRepoMock.Setup(x => x.HasActiveDispatchForAmbulance("AMB-01", "TP004")).Returns(false);
        _trsTransportRepoMock.Setup(x => x.HasActiveDispatchForDriver("DRV-01", "TP004")).Returns(false);

        var cmd = new DispatchTransportCmd("TP004", "AMB-01", "DRV-01", 1000m);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Maintenance*");
    }

    [Fact]
    public async Task GivenNegativeStartOdometer_WhenDispatched_ThenThrowsArgumentException()
    {
        // Arrange
        var cmd = new DispatchTransportCmd("TP005", "AMB-01", "DRV-01", -50m);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Start odometer*");
    }
}
