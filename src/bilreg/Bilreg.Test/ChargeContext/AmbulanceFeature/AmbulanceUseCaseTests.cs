using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class AmbulanceUseCaseTests
{
    private readonly Mock<IAmbulanceRepo> _ambulanceRepoMock = new();

    [Fact]
    public async Task AmbulanceSaveHandler_GivenNewAmbulance_CreatesAndSaves()
    {
        // Arrange
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe<AmbulanceModel>.None);

        var handler = new AmbulanceSaveHandler(_ambulanceRepoMock.Object);
        var cmd = new AmbulanceSaveCommand("AMB-01", "Ambulance Alfa", "B 1234 CD", "ALS", "Available", 50000m);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.AmbulanceId.Should().Be("AMB-01");
        _ambulanceRepoMock.Verify(x => x.SaveChanges(It.Is<AmbulanceModel>(a =>
            a.AmbulanceId == "AMB-01" &&
            a.AmbulanceName == "Ambulance Alfa" &&
            a.NoPolisi == "B 1234 CD" &&
            a.TipeArmada == "ALS" &&
            a.Abonement == 50000m)), Times.Once);
    }

    [Fact]
    public async Task AmbulanceSaveHandler_GivenExistingAmbulance_UpdatesAndSaves()
    {
        // Arrange
        var existing = new AmbulanceModel("AMB-01", "Old Name", "B 0000 XX");
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe.From(existing));

        var handler = new AmbulanceSaveHandler(_ambulanceRepoMock.Object);
        var cmd = new AmbulanceSaveCommand("AMB-01", "Updated Name", "B 9999 YY", "Jenazah", "Maintenance", 75000m);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.AmbulanceId.Should().Be("AMB-01");
        existing.AmbulanceName.Should().Be("Updated Name");
        existing.NoPolisi.Should().Be("B 9999 YY");
        existing.TipeArmada.Should().Be("Jenazah");
        existing.StatusArmada.Should().Be("Maintenance");
        existing.Abonement.Should().Be(75000m);
        _ambulanceRepoMock.Verify(x => x.SaveChanges(existing), Times.Once);
    }

    [Fact]
    public async Task AmbulanceAddKomponenHandler_AddsComponent()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance Alfa");
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe.From(ambulance));

        var handler = new AmbulanceAddKomponenHandler(_ambulanceRepoMock.Object);
        var cmd = new AmbulanceAddKomponenCommand("AMB-01", "KOMP-01", 100000m, true);

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        ambulance.ListKomponen.Should().HaveCount(1);
        ambulance.ListKomponen[0].KomponenId.Should().Be("KOMP-01");
        ambulance.ListKomponen[0].NilaiTarif.Should().Be(100000m);
        ambulance.ListKomponen[0].IsTetap.Should().BeTrue();
        _ambulanceRepoMock.Verify(x => x.SaveChanges(ambulance), Times.Once);
    }

    [Fact]
    public async Task AmbulanceRemoveKomponenHandler_RemovesComponent()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance Alfa");
        ambulance.Add(new AmbulanceKomponenModel("AMB-01", "KOMP-01", 100000m, true));
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe.From(ambulance));

        var handler = new AmbulanceRemoveKomponenHandler(_ambulanceRepoMock.Object);
        var cmd = new AmbulanceRemoveKomponenCommand("AMB-01", "KOMP-01");

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        ambulance.ListKomponen.Should().BeEmpty();
        _ambulanceRepoMock.Verify(x => x.SaveChanges(ambulance), Times.Once);
    }

    [Fact]
    public async Task AmbulanceAddTujuanHandler_AddsTujuanMapping()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance Alfa");
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe.From(ambulance));

        var handler = new AmbulanceAddTujuanHandler(_ambulanceRepoMock.Object);
        var cmd = new AmbulanceAddTujuanCommand("AMB-01", "TJ-01", 1.5m);

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        ambulance.ListTujuan.Should().HaveCount(1);
        ambulance.ListTujuan[0].TujuanTransportId.Should().Be("TJ-01");
        ambulance.ListTujuan[0].Konstanta.Should().Be(1.5m);
        _ambulanceRepoMock.Verify(x => x.SaveChanges(ambulance), Times.Once);
    }

    [Fact]
    public async Task AmbulanceRemoveTujuanHandler_RemovesTujuanMapping()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance Alfa");
        ambulance.AddTujuan(new TransportTujuanModel("AMB-01", "TJ-01", 1.5m));
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe.From(ambulance));

        var handler = new AmbulanceRemoveTujuanHandler(_ambulanceRepoMock.Object);
        var cmd = new AmbulanceRemoveTujuanCommand("AMB-01", "TJ-01");

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        ambulance.ListTujuan.Should().BeEmpty();
        _ambulanceRepoMock.Verify(x => x.SaveChanges(ambulance), Times.Once);
    }

    [Fact]
    public async Task AmbulanceActivate_And_Deactivate_UpdatesStatus()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance Alfa");
        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe.From(ambulance));

        var deactHandler = new AmbulanceDeactivateHandler(_ambulanceRepoMock.Object);
        var actHandler = new AmbulanceActivateHandler(_ambulanceRepoMock.Object);

        // Act & Assert Deactivate
        await deactHandler.Handle(new AmbulanceDeactivateCommand("AMB-01"), CancellationToken.None);
        ambulance.IsAktif.Should().BeFalse();

        // Act & Assert Activate
        await actHandler.Handle(new AmbulanceActivateCommand("AMB-01"), CancellationToken.None);
        ambulance.IsAktif.Should().BeTrue();
    }

    [Fact]
    public async Task AmbulanceGetHandler_ReturnsMappedResponse()
    {
        // Arrange
        var ambulance = new AmbulanceModel("AMB-01", "Ambulance Alfa", "B 1234 CD", "ALS", "Available");
        ambulance.SetAbonement(50000m);
        ambulance.Add(new AmbulanceKomponenModel("AMB-01", "KOMP-01", 100000m, true));
        ambulance.AddTujuan(new TransportTujuanModel("AMB-01", "TJ-01", 2.0m));

        _ambulanceRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IAmbulanceKey>()))
            .Returns(MayBe.From(ambulance));

        var handler = new AmbulanceGetHandler(_ambulanceRepoMock.Object);

        // Act
        var result = await handler.Handle(new AmbulanceGetQuery("AMB-01"), CancellationToken.None);

        // Assert
        result.AmbulanceId.Should().Be("AMB-01");
        result.AmbulanceName.Should().Be("Ambulance Alfa");
        result.NoPolisi.Should().Be("B 1234 CD");
        result.TipeArmada.Should().Be("ALS");
        result.Abonement.Should().Be(50000m);
        result.ListKomponen.Should().HaveCount(1);
        result.ListTujuan.Should().HaveCount(1);
    }

    [Fact]
    public async Task AmbulanceListHandler_ReturnsAllAmbulances()
    {
        // Arrange
        var amb1 = new AmbulanceModel("AMB-01", "Ambulance 1");
        var amb2 = new AmbulanceModel("AMB-02", "Ambulance 2");
        _ambulanceRepoMock.Setup(x => x.ListData()).Returns([amb1, amb2]);

        var handler = new AmbulanceListHandler(_ambulanceRepoMock.Object);

        // Act
        var result = await handler.Handle(new ListMasterAmbulanceQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }
}
