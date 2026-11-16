using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class TujuanTransportRepoTest
{
    private readonly Mock<ITujuanTransportDal> _tujuanTransportDalMock;
    private readonly TujuanTransportRepo _sut;

    public TujuanTransportRepoTest()
    {
        _tujuanTransportDalMock = new Mock<ITujuanTransportDal>();
        _sut = new TujuanTransportRepo(_tujuanTransportDalMock.Object);
    }

    [Fact]
    public void SaveChanges_WhenNew_Inserts()
    {
        // Arrange
        var model = new TujuanTransportModel("TJ01", "RS Luar Kota", 2.5m, true);
        model.SetDefaultAmbulance("A01");
        _tujuanTransportDalMock.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>())).Returns((TujuanTransportModel)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _tujuanTransportDalMock.Verify(x => x.Insert(model), Times.Once);
        _tujuanTransportDalMock.Verify(x => x.Update(It.IsAny<TujuanTransportModel>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_WhenExisting_Updates()
    {
        // Arrange
        var model = new TujuanTransportModel("TJ01", "RS Luar Kota Update", 3.0m, false);
        var existing = new TujuanTransportModel("TJ01", "RS Lama", 1.0m, false);
        _tujuanTransportDalMock.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>())).Returns(existing);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _tujuanTransportDalMock.Verify(x => x.Update(model), Times.Once);
        _tujuanTransportDalMock.Verify(x => x.Insert(It.IsAny<TujuanTransportModel>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenNotFound_ReturnsNone()
    {
        // Arrange
        _tujuanTransportDalMock.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>())).Returns((TujuanTransportModel)null!);

        // Act
        var result = _sut.LoadEntity("TJ99");

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadEntity_WhenFound_ReturnsModel()
    {
        // Arrange
        var model = new TujuanTransportModel("TJ01", "Bandara", 1.0m, false);
        _tujuanTransportDalMock.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>())).Returns(model);

        // Act
        var result = _sut.LoadEntity(new TujuanTransportKey("TJ01"));

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.TujuanTransportName.Should().Be("Bandara");
    }

    [Fact]
    public void Delete_DeletesEntity()
    {
        // Arrange
        var key = new TujuanTransportKey("TJ01");

        // Act
        _sut.Delete(key);

        // Assert
        _tujuanTransportDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void ListData_ReturnsAll()
    {
        // Arrange
        var list = new List<TujuanTransportModel>
        {
            new("TJ01", "Tujuan 1", 1m, false),
            new("TJ02", "Tujuan 2", 2m, true)
        };
        _tujuanTransportDalMock.Setup(x => x.ListData()).Returns(list);

        // Act
        var result = _sut.ListData();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void TujuanTransportDto_ToModel_MapsAllProperties()
    {
        // Arrange
        var dto = new TujuanTransportDto
        {
            fs_kd_tujuan_transport = "TJ01",
            fs_nm_tujuan_transport = "Stasiun",
            fn_konstanta = 1.25m,
            fb_perkiraan = true,
            fs_kd_default_transport = "A02"
        };

        // Act
        var model = dto.ToModel();

        // Assert
        model.TujuanTransportId.Should().Be("TJ01");
        model.TujuanTransportName.Should().Be("Stasiun");
        model.Konstanta.Should().Be(1.25m);
        model.IsPerkiraan.Should().BeTrue();
        model.DefaultAmbulanceId.Should().Be("A02");
    }
}
