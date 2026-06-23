using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapRepoTest
{
    private readonly Mock<IAntrianMapDal> _antrianMapDalMock;
    private readonly Mock<IAntrianMapDetilDal> _antrianMapDetilDalMock;
    private readonly AntrianMapRepo _sut;

    public AntrianMapRepoTest()
    {
        _antrianMapDalMock = new Mock<IAntrianMapDal>();
        _antrianMapDetilDalMock = new Mock<IAntrianMapDetilDal>();
        _sut = new AntrianMapRepo(_antrianMapDalMock.Object, _antrianMapDetilDalMock.Object);
    }

    // ─────────────────────────────────────────────────────
    //  HELPERS / BUILDERS
    // ─────────────────────────────────────────────────────

    private static AntrianMapDto BuildHdrDto(
        string antrianMapId = "AM-001",
        string jadwalId = "J-001",
        string kdDokter = "D-001",
        string nmDokter = "Dr. One",
        string kdLayanan = "L-001",
        string nmLayanan = "Layanan One",
        string pattern = "P1",
        int max = 10)
        => new AntrianMapDto(
            fs_kd_antrian_map: antrianMapId,
            fs_kd_jadwal: jadwalId,
            fs_kd_jadwal_harian: null,
            fs_kd_dokter: kdDokter,
            fs_kd_layanan: kdLayanan,
            fd_tgl_jadwal: new DateTime(2026, 4, 23),
            fs_jam_jadwal: "08:00",
            fs_jam_praktek: "08:30",
            fs_pattern: pattern,
            fn_max: max,
            fs_nm_dokter: nmDokter,
            fs_nm_layanan: nmLayanan);

    private static AntrianMapDetilDto BuildDetilDto(
        int noUrut = 1,
        string antrianMapId = "AM-001",
        string kdDokter = "D-001",
        string kdLayanan = "L-001",
        string mr = "P-001",
        string nmPasien = "John Doe",
        string flag = "A",
        bool terpakai = false)
        => new AntrianMapDetilDto(
            fs_kd_antrian_map: antrianMapId,
            fs_kd_dokter: kdDokter,
            fs_kd_layanan: kdLayanan,
            fd_tgl_jadwal: "2026-04-23",
            fs_jam_jadwal: "08:00",
            fn_no_antrian: noUrut,
            fs_flag: flag,
            fs_mr: mr,
            fs_nm_pasien: nmPasien,
            fs_kd_trs_gen: $"REF-{noUrut}",
            fb_terpakai: terpakai,
            fs_nm_dokter: "Dr. One",
            fs_nm_layanan: "Layanan One");

    private static IAntrianMapKey Key(string id = "AM-001")
        => AntrianMapModel.Key(id);

    private static ILayananKey LayananKey(string id = "L-001")
        => LayananType.Key(id);

    private static IPpaKey PpaKey(string id = "D-001")
        => PpaType.Key(id);

    [Fact]
    public void LoadEntity_ShouldReturnModelWithDetails_WhenHeaderAndDetailsExist()
    {
        // Arrange
        var key = Key();
        var hdrDto = BuildHdrDto();
        var detilDtos = new List<AntrianMapDetilDto>
        {
            BuildDetilDto(noUrut: 1, mr: "P-001"),
            BuildDetilDto(noUrut: 2, mr: "P-002")
        };

        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Returns(hdrDto);

        _antrianMapDetilDalMock
            .Setup(x => x.ListData(It.IsAny<IAntrianMapKey>()))
            .Returns(detilDtos);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.TotalSlotCount.Should().Be(2),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void LoadEntity_ShouldReturnModelWithNoDetails_WhenDetailDalReturnsNull()
    {
        // Arrange
        var key = Key();
        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Returns(BuildHdrDto());

        _antrianMapDetilDalMock
            .Setup(x => x.ListData(It.IsAny<IAntrianMapKey>()))
            .Returns((IEnumerable<AntrianMapDetilDto>)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.TotalSlotCount.Should().Be(0),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void LoadEntity_ShouldReturnModelWithNoDetails_WhenDetailDalReturnsEmptyList()
    {
        // Arrange
        var key = Key();
        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Returns(BuildHdrDto());

        _antrianMapDetilDalMock
            .Setup(x => x.ListData(It.IsAny<IAntrianMapKey>()))
            .Returns(new List<AntrianMapDetilDto>());

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.TotalSlotCount.Should().Be(0),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void LoadEntity_ShouldReturnNone_WhenHeaderNotFound()
    {
        // Arrange
        var key = Key();
        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Returns((AntrianMapDto)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: _ => Assert.Fail("Expected None but got Some"),
            onNone: () => { });
    }

    [Fact]
    public void LoadEntity_ShouldPreserveHeaderFieldValues_WhenDataExists()
    {
        // Arrange
        var key = Key("AM-099");
        var hdrDto = BuildHdrDto(antrianMapId: "AM-099", jadwalId: "J-099", pattern: "PTN", max: 25);

        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Returns(hdrDto);

        _antrianMapDetilDalMock
            .Setup(x => x.ListData(It.IsAny<IAntrianMapKey>()))
            .Returns(new List<AntrianMapDetilDto>());

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.AntrianMapId.Should().Be("AM-099");
                model.JadwalId.Should().Be("J-099");
                model.AntrianPattern.Tipe.Should().Be("PTN");
                model.MaxPasien.Should().Be(25);
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void LoadEntity_ShouldPropagateException_WhenDalThrows()
    {
        // Arrange
        var key = Key();
        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Throws<InvalidOperationException>();

        // Act
        var act = () => _sut.LoadEntity(key);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SaveChanges_ShouldInsertHeader_WhenHeaderNotFound()
    {
        // Arrange
        var model = BuildAntrianMapModel(detilCount: 2);
        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Returns((AntrianMapDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _antrianMapDalMock.Verify(x => x.Insert(It.IsAny<AntrianMapDto>()), Times.Once);
        _antrianMapDalMock.Verify(x => x.Update(It.IsAny<AntrianMapDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_ShouldUpdateHeader_WhenHeaderExists()
    {
        // Arrange
        var model = BuildAntrianMapModel(detilCount: 2);
        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Returns(BuildHdrDto());

        // Act
        _sut.SaveChanges(model);

        // Assert
        _antrianMapDalMock.Verify(x => x.Update(It.IsAny<AntrianMapDto>()), Times.Once);
        _antrianMapDalMock.Verify(x => x.Insert(It.IsAny<AntrianMapDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_ShouldDeleteAndReInsertDetails_WhenCalled()
    {
        // Arrange
        const int detilCount = 3;
        var model = BuildAntrianMapModel(detilCount: detilCount);
        _antrianMapDalMock
            .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
            .Returns(BuildHdrDto());

        // Act
        _sut.SaveChanges(model);

        // Assert
        _antrianMapDetilDalMock.Verify(x => x.Delete(It.IsAny<IAntrianMapKey>()), Times.Once);
        _antrianMapDetilDalMock.Verify(x => x.Insert(It.IsAny<AntrianMapDetilDto>()), Times.Exactly(detilCount));
    }

    [Fact]
    public void ListData_ShouldReturnViews_WhenDalReturnsRecords()
    {
        // Arrange
        var lynKey = LayananKey();
        var ppaKey = PpaKey();
        var tgl = new DateOnly(2026, 4, 23);

        var dtos = new List<AntrianMapDto>
        {
            BuildHdrDto(antrianMapId: "AM-001"),
            BuildHdrDto(antrianMapId: "AM-002")
        };

        _antrianMapDalMock
            .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), It.IsAny<DateOnly>()))
            .Returns(dtos);

        // Act
        var result = _sut.ListData(lynKey, ppaKey, tgl).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void ListData_ShouldReturnEmpty_WhenDalReturnsEmptyList()
    {
        // Arrange
        _antrianMapDalMock
            .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), It.IsAny<DateOnly>()))
            .Returns(new List<AntrianMapDto>());

        // Act
        var result = _sut.ListData(LayananKey(), PpaKey(), new DateOnly(2026, 4, 23)).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ListData_ShouldReturnEmpty_WhenDalReturnsNull()
    {
        // Arrange
        _antrianMapDalMock
            .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), It.IsAny<DateOnly>()))
            .Returns((IEnumerable<AntrianMapDto>)null!);

        // Act
        var result = _sut.ListData(LayananKey(), PpaKey(), new DateOnly(2026, 4, 23)).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    private static AntrianMapModel BuildAntrianMapModel(int detilCount = 0)
    {
        var detils = Enumerable.Range(1, detilCount)
            .Select(i => new AntrianMapDetilModel(i, $"Pasien-{i}", $"MR-{i}", $"REF-{i}", "A", false))
            .ToList();

        return new AntrianMapModel(
            antrianMapId: "AM-001",
            jadwalId: "J-001",
            dokter: new PpaReff("D-001", "Dr. One"),
            layanan: new LayananReff("L-001", "Layanan One"),
            tglJadwal: new DateOnly(2026, 4, 23),
            jamJadwal: new TimeOnly(8, 0),
            jamPraktek: new TimeOnly(8, 30),
            pattern: AntrianPatternType.Default,
            maxPasien: 10,
            listMap: detils);
    }
}