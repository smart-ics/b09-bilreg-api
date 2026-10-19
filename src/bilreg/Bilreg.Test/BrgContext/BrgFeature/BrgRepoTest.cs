using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BrgContext.BrgFeature;

public class BrgRepoTests
{
    private readonly Mock<IBrgDal> _brgDalMock;
    private readonly Mock<IBrgSatuanDal> _brgSatuanDalMock;
    private readonly BrgRepo _repository;

    public BrgRepoTests()
    {
        _brgDalMock = new Mock<IBrgDal>();
        _brgSatuanDalMock = new Mock<IBrgSatuanDal>();
        _repository = new BrgRepo(_brgDalMock.Object, _brgSatuanDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingBrgObatEntity_WhenSaveChanges_ThenUpdateAndDeleteInsertAreCalled()
    {
        // Arrange
        var existingModel = CreateTestBrgObatModel();
        _brgDalMock
            .Setup(x => x.GetData(It.IsAny<IBrgKey>()))
            .Returns(CreateTestBrgDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Once);
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.IsAny<IBrgKey>()), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BrgSatuanDto>>()), Times.Once);
    }

    [Fact]
    public void UT2_GivenExistingBrgBhpEntity_WhenSaveChanges_ThenUpdateAndDeleteInsertAreCalled()
    {
        // Arrange
        var existingModel = CreateTestBrgBhpModel();
        _brgDalMock
            .Setup(x => x.GetData(It.IsAny<IBrgKey>()))
            .Returns(CreateTestBrgDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Once);
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.IsAny<IBrgKey>()), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BrgSatuanDto>>()), Times.Once);
    }

    [Fact]
    public void UT3_GivenNewBrgObatEntity_WhenSaveChanges_ThenInsertAndDeleteInsertAreCalled()
    {
        // Arrange
        var newModel = CreateTestBrgObatModel();
        var key = CreateTestKey();
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns((BrgDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Once);
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.IsAny<IBrgKey>()), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BrgSatuanDto>>()), Times.Once);
    }

    [Fact]
    public void UT4_GivenNewBrgBhpEntity_WhenSaveChanges_ThenInsertAndDeleteInsertAreCalled()
    {
        // Arrange
        var newModel = CreateTestBrgBhpModel();
        var key = CreateTestKey();
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns((BrgDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Once);
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.IsAny<IBrgKey>()), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<BrgSatuanDto>>()), Times.Once);
    }

    [Fact]
    public void UT5_GivenExistingBrgObatEntity_WhenLoadEntity_ThenBarangModelIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestBrgDtoWithGroup("BRG01", "OBT");
        var key = new BrgKey("BRG01");
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        var satuanDtos = new List<BrgSatuanDto> { CreateTestBrgSatuanDto() };
        _brgSatuanDalMock
            .Setup(x => x.ListData(key))
            .Returns(satuanDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.Should().NotBeNull();
                model.Should().BeOfType<BarangModel>();
                model.GroupRekDk.GroupRekDkId.Should().Be("OBT");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT6_GivenExistingBrgBhpEntity_WhenLoadEntity_ThenBarangModelIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestBrgDtoWithGroup("BRG02", "BHP");
        var key = new BrgKey("BRG02");
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        var satuanDtos = new List<BrgSatuanDto> { CreateTestBrgSatuanDto() };
        _brgSatuanDalMock
            .Setup(x => x.ListData(key))
            .Returns(satuanDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.Should().NotBeNull();
                model.Should().BeOfType<BarangModel>();
                model.GroupRekDk.GroupRekDkId.Should().Be("BHP");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT7_GivenNonExistingEntity_WhenLoadEntity_ThenNoneIsReturned()
    {
        // Arrange
        var key = CreateTestKey();
        _brgDalMock
            .Setup(x => x.GetData(key))
            .Returns((BrgDto)null!);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: _ => Assert.Fail("Expected None but got Some"),
            onNone: () => { });
    }

    [Fact]
    public void UT8_GivenKey_WhenDeleteEntity_ThenDeleteIsCalled()
    {
        // Arrange
        var key = CreateTestKey();

        // Act
        _repository.DeleteEntity(key);

        // Assert
        _brgSatuanDalMock.Verify(x => x.Delete(key), Times.Once);
        _brgDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT9_GivenKeyword_WhenListData_ThenListWithViewsIsReturned()
    {
        // Arrange
        var keyword = "test";
        var listView = new List<BrgView> { CreateTestBrgView() };
        _brgDalMock
            .Setup(x => x.ListData(keyword))
            .Returns(listView);

        // Act
        var result = _repository.ListData(keyword);

        // Assert
        var brgViews = result.ToList();
        brgViews.Should().NotBeNull();
        brgViews.Count.Should().Be(1);
    }

    [Fact]
    public void UT10_GivenExistingAtkEntity_WhenLoadEntity_ThenBarangModelWithAtkIsReturnedWithoutException()
    {
        // Arrange
        var key = new BrgKey("ATK001");
        var dto = CreateTestBrgDtoWithGroup("ATK001", "ATK");
        _brgDalMock.Setup(x => x.GetData(key)).Returns(dto);
        _brgSatuanDalMock.Setup(x => x.ListData(key)).Returns(new List<BrgSatuanDto>
        {
            new("ATK001", "RIM", 1, "Rim")
        });

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.Should().BeOfType<BarangModel>();
                model.BrgId.Should().Be("ATK001");
                model.GroupRekDk.GroupRekDkId.Should().Be("ATK");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT11_GivenExistingGizEntity_WhenLoadEntity_ThenBarangModelWithGizIsReturnedWithoutException()
    {
        // Arrange
        var key = new BrgKey("GIZ001");
        var dto = CreateTestBrgDtoWithGroup("GIZ001", "GIZ");
        _brgDalMock.Setup(x => x.GetData(key)).Returns(dto);
        _brgSatuanDalMock.Setup(x => x.ListData(key)).Returns(new List<BrgSatuanDto>
        {
            new("GIZ001", "KG", 1, "Kilogram")
        });

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.Should().BeOfType<BarangModel>();
                model.BrgId.Should().Be("GIZ001");
                model.GroupRekDk.GroupRekDkId.Should().Be("GIZ");
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT12_GivenSingleSatuan_WhenLoadEntity_ThenSatuanKecilEqualsSatuanBesarAndKonversiIsOne()
    {
        // Arrange
        var key = new BrgKey("BRG_SINGLE");
        var dto = CreateTestBrgDtoWithGroup("BRG_SINGLE", "OBT");
        _brgDalMock.Setup(x => x.GetData(key)).Returns(dto);
        _brgSatuanDalMock.Setup(x => x.ListData(key)).Returns(new List<BrgSatuanDto>
        {
            new("BRG_SINGLE", "PCS", 1, "Pcs")
        });

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                var barang = (BarangModel)model;
                barang.SatuanKecil.SatuanId.Should().Be("PCS");
                barang.SatuanBesar.SatuanId.Should().Be("PCS");
                barang.Konversi.Should().Be(1);
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT13_GivenTieredSatuan_WhenLoadEntity_ThenSmallestKonversiIsSatuanKecilAndLargestIsSatuanBesar()
    {
        // Arrange: list provided with larger conversion first to test ordering
        var key = new BrgKey("BRG_TIER");
        var dto = CreateTestBrgDtoWithGroup("BRG_TIER", "OBT");
        _brgDalMock.Setup(x => x.GetData(key)).Returns(dto);
        _brgSatuanDalMock.Setup(x => x.ListData(key)).Returns(new List<BrgSatuanDto>
        {
            new("BRG_TIER", "BOX", 24, "Box"),
            new("BRG_TIER", "PCS", 1, "Pcs")
        });

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                var barang = (BarangModel)model;
                barang.SatuanKecil.SatuanId.Should().Be("PCS");
                barang.SatuanBesar.SatuanId.Should().Be("BOX");
                barang.Konversi.Should().Be(24);
                barang.ListSatuan.Should().HaveCount(2);
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT14_GivenNewBarangModel_WhenSaveChanges_ThenInsertHeaderAndSatuanBulk()
    {
        // Arrange
        var model = CreateTestBarangModel("BRG_NEW", "Barang Baru", "OBT", "PCS", "BOX", 10);
        _brgDalMock
            .Setup(x => x.GetData(It.Is<IBrgKey>(k => k.BrgId == "BRG_NEW")))
            .Returns((BrgDto)null!);

        // Act
        _repository.SaveChanges(model);

        // Assert
        _brgDalMock.Verify(x => x.Insert(It.Is<BrgDto>(d => d.fs_kd_barang == "BRG_NEW" && d.fs_nm_barang == "Barang Baru")), Times.Once);
        _brgDalMock.Verify(x => x.Update(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.Is<IBrgKey>(k => k.BrgId == "BRG_NEW")), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.Is<IEnumerable<BrgSatuanDto>>(list =>
            list.Count() == 2 &&
            list.Any(s => s.fs_kd_satuan == "PCS" && s.fn_nilai == 1) &&
            list.Any(s => s.fs_kd_satuan == "BOX" && s.fn_nilai == 10)
        )), Times.Once);
    }

    [Fact]
    public void UT15_GivenExistingBarangModel_WhenSaveChanges_ThenUpdateHeaderAndDeleteInsertSatuan()
    {
        // Arrange
        var model = CreateTestBarangModel("BRG_EXIST", "Barang Edit", "BHP", "PCS", "PCS", 1);
        _brgDalMock
            .Setup(x => x.GetData(It.Is<IBrgKey>(k => k.BrgId == "BRG_EXIST")))
            .Returns(CreateTestBrgDtoWithGroup("BRG_EXIST", "BHP"));

        // Act
        _repository.SaveChanges(model);

        // Assert
        _brgDalMock.Verify(x => x.Update(It.Is<BrgDto>(d => d.fs_kd_barang == "BRG_EXIST" && d.fs_nm_barang == "Barang Edit")), Times.Once);
        _brgDalMock.Verify(x => x.Insert(It.IsAny<BrgDto>()), Times.Never);
        _brgSatuanDalMock.Verify(x => x.Delete(It.Is<IBrgKey>(k => k.BrgId == "BRG_EXIST")), Times.Once);
        _brgSatuanDalMock.Verify(x => x.Insert(It.Is<IEnumerable<BrgSatuanDto>>(list =>
            list.Count() == 1 &&
            list.Any(s => s.fs_kd_satuan == "PCS" && s.fn_nilai == 1)
        )), Times.Once);
    }

    private static BarangModel CreateTestBarangModel(
        string id,
        string name,
        string groupRekDkId,
        string satuanKecilId,
        string satuanBesarId,
        int konversi)
    {
        return BarangModel.Create(
            id,
            name,
            true,
            "Ket",
            new GroupRekReff("GR01", "Grup 01"),
            new GroupRekDkType(groupRekDkId, groupRekDkId),
            new PabrikReff("PB01", "Pabrik 01"),
            new SatuanType(satuanKecilId, satuanKecilId),
            new SatuanType(satuanBesarId, satuanBesarId),
            konversi);
    }

    private static BrgDto CreateTestBrgDtoWithGroup(string brgId, string groupRekDkId)
    {
        return new BrgDto(
            brgId,
            "Barang Test",
            true,
            "Ket",
            "GR01",
            "PB01",
            "Grup 01",
            groupRekDkId,
            $"Grup {groupRekDkId}",
            "Pabrik 01");
    }

    private static BrgObatType CreateTestBrgObatModel()
        => BrgObatType.Default;

    private static BrgBhpType CreateTestBrgBhpModel()
        => BrgBhpType.Default;

    private static BrgDto CreateTestBrgDto()
        => BrgDto.FromModel(BrgObatType.Default);

    private static BrgSatuanDto CreateTestBrgSatuanDto()
        => BrgSatuanDto.FromModel("A", BrgSatuanType.Default);

    private static BrgView CreateTestBrgView()
        => BrgView.FromModel(BrgObatType.Default);

    private static IBrgKey CreateTestKey()
        => BrgObatType.Key("A");
}