using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class AmbulanceRepoTest
{
    private readonly Mock<IAmbulanceDal> _ambulanceDalMock;
    private readonly Mock<IAmbulanceKomponenDal> _ambulanceKomponenDalMock;
    private readonly Mock<ITransportTujuanDal> _transportTujuanDalMock;
    private readonly AmbulanceRepo _sut;

    public AmbulanceRepoTest()
    {
        _ambulanceDalMock = new Mock<IAmbulanceDal>();
        _ambulanceKomponenDalMock = new Mock<IAmbulanceKomponenDal>();
        _transportTujuanDalMock = new Mock<ITransportTujuanDal>();
        _sut = new AmbulanceRepo(
            _ambulanceDalMock.Object,
            _ambulanceKomponenDalMock.Object,
            _transportTujuanDalMock.Object);
    }

    [Fact]
    public void SaveChanges_WhenNew_InsertsAmbulanceKomponenAndTujuan()
    {
        // Arrange
        var model = new AmbulanceModel("A01", "Ambulance Jenazah", "B 1234 CD", "Jenazah", "Available");
        model.SetAbonement(100000m);
        model.Add(new AmbulanceKomponenModel("A01", "K01", 50000m, true));
        model.AddTujuan(new TransportTujuanModel("A01", "TJ01", 1.5m));

        _ambulanceDalMock.Setup(x => x.GetData(It.IsAny<IAmbulanceKey>())).Returns((AmbulanceModel)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _ambulanceDalMock.Verify(x => x.Insert(model), Times.Once);
        _ambulanceDalMock.Verify(x => x.Update(It.IsAny<AmbulanceModel>()), Times.Never);
        _ambulanceKomponenDalMock.Verify(x => x.Delete(model), Times.Once);
        _ambulanceKomponenDalMock.Verify(x => x.Insert(model.ListKomponen), Times.Once);
        _transportTujuanDalMock.Verify(x => x.Delete(model), Times.Once);
        _transportTujuanDalMock.Verify(x => x.Insert(model.ListTujuan), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenExisting_UpdatesAmbulanceAndReplacesDetails()
    {
        // Arrange
        var model = new AmbulanceModel("A01", "Ambulance ALS", "B 5678 EF", "ALS", "InUse");
        var existing = new AmbulanceModel("A01", "Ambulance Lama");
        _ambulanceDalMock.Setup(x => x.GetData(It.IsAny<IAmbulanceKey>())).Returns(existing);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _ambulanceDalMock.Verify(x => x.Update(model), Times.Once);
        _ambulanceDalMock.Verify(x => x.Insert(It.IsAny<AmbulanceModel>()), Times.Never);
        _ambulanceKomponenDalMock.Verify(x => x.Delete(model), Times.Once);
        _transportTujuanDalMock.Verify(x => x.Delete(model), Times.Once);
    }

    [Fact]
    public void LoadEntity_WhenNotFound_ReturnsNone()
    {
        // Arrange
        _ambulanceDalMock.Setup(x => x.GetData(It.IsAny<IAmbulanceKey>())).Returns((AmbulanceModel)null!);

        // Act
        var result = _sut.LoadEntity("A99");

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadEntity_WhenFound_AttachesKomponenAndTujuan()
    {
        // Arrange
        var model = new AmbulanceModel("A01", "Ambulance Transport", "B 9999 ZZ", "Transport", "Available");
        _ambulanceDalMock.Setup(x => x.GetData(It.IsAny<IAmbulanceKey>())).Returns(model);
        _ambulanceKomponenDalMock.Setup(x => x.ListData(It.IsAny<IAmbulanceKey>()))
            .Returns([new AmbulanceKomponenModel("A01", "K01", 75000m, true)]);
        _transportTujuanDalMock.Setup(x => x.ListData(It.IsAny<IAmbulanceKey>()))
            .Returns([new TransportTujuanModel("A01", "TJ01", 2.0m)]);

        // Act
        var result = _sut.LoadEntity(new AmbulanceKey("A01"));

        // Assert
        result.HasValue.Should().BeTrue();
        var entity = result.Value;
        entity.AmbulanceId.Should().Be("A01");
        entity.ListKomponen.Should().HaveCount(1);
        entity.ListKomponen[0].KomponenId.Should().Be("K01");
        entity.ListTujuan.Should().HaveCount(1);
        entity.ListTujuan[0].TujuanTransportId.Should().Be("TJ01");
    }

    [Fact]
    public void Delete_DeletesFromAllThreeTables()
    {
        // Arrange
        var key = new AmbulanceKey("A01");

        // Act
        _sut.Delete(key);

        // Assert
        _transportTujuanDalMock.Verify(x => x.Delete(key), Times.Once);
        _ambulanceKomponenDalMock.Verify(x => x.Delete(key), Times.Once);
        _ambulanceDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void ListData_ReturnsAllWithDetails()
    {
        // Arrange
        var a1 = new AmbulanceModel("A01", "Amb 1");
        var a2 = new AmbulanceModel("A02", "Amb 2");
        _ambulanceDalMock.Setup(x => x.ListData()).Returns([a1, a2]);
        _ambulanceKomponenDalMock.Setup(x => x.ListData(It.IsAny<IAmbulanceKey>())).Returns([]);
        _transportTujuanDalMock.Setup(x => x.ListData(It.IsAny<IAmbulanceKey>())).Returns([]);

        // Act
        var result = _sut.ListData().ToList();

        // Assert
        result.Should().HaveCount(2);
        _ambulanceKomponenDalMock.Verify(x => x.ListData(It.IsAny<IAmbulanceKey>()), Times.Exactly(2));
    }

    [Fact]
    public void AmbulanceDto_ToModel_MapsAllProperties()
    {
        // Arrange
        var dto = new AmbulanceDto
        {
            fs_kd_transport = "A01",
            fs_nm_transport = "Ambulans Siaga",
            fb_aktif = false,
            fn_abonemen = 50000m,
            fs_no_polisi = "AB 1234 CD",
            fs_tipe_armada = "ALS",
            fs_status_armada = "Maintenance"
        };

        // Act
        var model = dto.ToModel();

        // Assert
        model.AmbulanceId.Should().Be("A01");
        model.AmbulanceName.Should().Be("Ambulans Siaga");
        model.IsAktif.Should().BeFalse();
        model.Abonement.Should().Be(50000m);
        model.NoPolisi.Should().Be("AB 1234 CD");
        model.TipeArmada.Should().Be("ALS");
        model.StatusArmada.Should().Be(AmbulanceStatusEnum.Maintenance);
    }
}
