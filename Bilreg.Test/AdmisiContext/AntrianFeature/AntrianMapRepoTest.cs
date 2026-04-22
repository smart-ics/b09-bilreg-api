using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapRepoTest
{
    private readonly Mock<IAntrianMapDal> _antrianMapDal;
    private readonly Mock<IAntrianMapDetilDal> _antrianMapDetilDal;
    private readonly AntrianMapRepo _sut;

    public AntrianMapRepoTest()
    {
        _antrianMapDal     = new Mock<IAntrianMapDal>();
        _antrianMapDetilDal = new Mock<IAntrianMapDetilDal>();
        _sut = new AntrianMapRepo(_antrianMapDal.Object, _antrianMapDetilDal.Object);
    }

    private static AntrianMapModel BuildModel(
        string id          = "AM01",
        string jadwalId    = "J01",
        string dokterId    = "D01",
        string dokterName  = "Dr. Satu",
        string layananId   = "L01",
        string layananName = "Poli Umum",
        IEnumerable<AntrianMapDetilModel>? listDetil = null)
    {
        var tgl     = new DateOnly(2026, 4, 22);
        var jam     = new TimeOnly(8, 0);
        var praktek = new TimeOnly(8, 30);
        return new AntrianMapModel(
            antrianMapId: id,
            jadwalId:     jadwalId,
            dokter:       new PpaReff(dokterId, dokterName),
            layanan:      new LayananReff(layananId, layananName),
            tglJadwal:    tgl,
            jamJadwal:    jam,
            jamPraktek:   praktek,
            pattern:      "P1",
            maxPasien:    10,
            listMap:      listDetil ?? BuildDetilList()
        );
    }

    private static List<AntrianMapDetilModel> BuildDetilList() =>
    [
        new(1, "Pasien Satu", "MR001", "REF01", "N", false),
        new(2, "Pasien Dua",  "MR002", "REF02", "N", true)
    ];

    private static AntrianMapDto BuildDto(
        string id          = "AM01",
        string jadwalId    = "J01",
        string dokterId    = "D01",
        string layananId   = "L01",
        string dokterName  = "Dr. Satu",
        string layananName = "Poli Umum")
        => new AntrianMapDto(
            fs_kd_antrian_map: id,
            fs_kd_jadwal:      jadwalId,
            fs_kd_dokter:      dokterId,
            fs_kd_layanan:     layananId,
            fd_tgl_jadwal:     new DateTime(2026, 4, 22),
            fs_jam_jadwal:     "08:00",
            fs_jam_praktek:    "08:30",
            fs_pattern:        "P1",
            fn_max:            10,
            fs_nm_dokter:      dokterName,
            fs_nm_layanan:     layananName);

    private static AntrianMapDetilDto BuildDetilDto(
        string mapId      = "AM01",
        int    noAntrian  = 1,
        string pasienName = "Pasien Satu",
        string mr         = "MR001",
        string reffId     = "REF01",
        bool   terpakai   = false)
        => new AntrianMapDetilDto(
            fs_kd_antrian_map: mapId,
            fs_kd_dokter:      "D01",
            fs_kd_layanan:     "L01",
            fd_tgl_jadwal:     "2026-04-22",
            fs_jam_jadwal:     "08:00",
            fn_no_antrian:     noAntrian,
            fs_flag:           "N",
            fs_mr:             mr,
            fs_nm_pasien:      pasienName,
            fs_kd_trs_gen:     reffId,
            fb_terpakai:       terpakai,
            fs_nm_dokter:      "Dr. Satu",
            fs_nm_layanan:     "Poli Umum");

    private static IAntrianMapKey BuildKey(string id = "AM01")
        => AntrianMapModel.Key(id);

    private static IPpaKey BuildPpaKey(string id = "D01")
        => PpaType.Key(id);

    private static ILayananKey BuildLayananKey(string id = "L01")
        => LayananType.Key(id);

    public class SaveChangesTests : AntrianMapRepoTest
    {
        [Fact]
        public void SaveChanges_ShouldCallInsert_WhenAntrianNotFound()
        {
            // Arrange
            var model = BuildModel();
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns((AntrianMapDto)null!);

            // Act
            _sut.SaveChanges(model);

            // Assert
            _antrianMapDal.Verify(x => x.Insert(It.IsAny<AntrianMapDto>()), Times.Once);
            _antrianMapDal.Verify(x => x.Update(It.IsAny<AntrianMapDto>()), Times.Never);
        }

        [Fact]
        public void SaveChanges_ShouldCallUpdate_WhenAntrianExist()
        {
            // Arrange
            var model       = BuildModel();
            var existingDto = BuildDto();
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns(existingDto);

            // Act
            _sut.SaveChanges(model);

            // Assert
            _antrianMapDal.Verify(x => x.Update(It.IsAny<AntrianMapDto>()), Times.Once);
            _antrianMapDal.Verify(x => x.Insert(It.IsAny<AntrianMapDto>()), Times.Never);
        }

        [Fact]
        public void SaveChanges_ShouldDeleteThenInsertEachDetil_WhenAntrianHasListDetil()
        {
            // Arrange – model has 2 detil items
            var detils = BuildDetilList();
            var model  = BuildModel(listDetil: detils);
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns((AntrianMapDto)null!);

            // Act
            _sut.SaveChanges(model);

            // Assert
            _antrianMapDetilDal.Verify(x => x.Delete(It.IsAny<IAntrianMapKey>()), Times.Once);
            _antrianMapDetilDal.Verify(x => x.Insert(It.IsAny<AntrianMapDetilDto>()), Times.Exactly(detils.Count));
        }

        [Fact]
        public void SaveChanges_ShouldDeleteAndNotInsertDetil_WhenAntrianHasNoListDetil()
        {
            // Arrange – model with zero detil items
            var model = BuildModel(listDetil: []);
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns((AntrianMapDto)null!);

            // Act
            _sut.SaveChanges(model);

            // Assert
            _antrianMapDetilDal.Verify(x => x.Delete(It.IsAny<IAntrianMapKey>()), Times.Once);
            _antrianMapDetilDal.Verify(x => x.Insert(It.IsAny<AntrianMapDetilDto>()), Times.Never);
        }

        [Fact]
        public void SaveChanges_ShouldInsertDtoWithCorrectAntrianMapId_WhenAntrianIsNew()
        {
            // Arrange
            var model = BuildModel(id: "AM-NEW");
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns((AntrianMapDto)null!);

            // Act
            _sut.SaveChanges(model);

            // Assert
            _antrianMapDal.Verify(
                x => x.Insert(It.Is<AntrianMapDto>(dto => dto.fs_kd_antrian_map == "AM-NEW")),
                Times.Once);
        }

        [Fact]
        public void SaveChanges_ShouldUpdateDtoWithCorrectAntrianMapId_WhenModelExists()
        {
            // Arrange
            var model = BuildModel(id: "AM-EXIST");
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns(BuildDto(id: "AM-EXIST"));

            // Act
            _sut.SaveChanges(model);

            // Assert
            _antrianMapDal.Verify(
                x => x.Update(It.Is<AntrianMapDto>(dto => dto.fs_kd_antrian_map == "AM-EXIST")),
                Times.Once);
        }
    }

    public class LoadEntityTests : AntrianMapRepoTest
    {
        [Fact]
        public void LoadEntity_ShouldReturnModel_WhenKeyExists()
        {
            // Arrange
            var key         = BuildKey();
            var dto         = BuildDto();
            var detilDtos   = new List<AntrianMapDetilDto> { BuildDetilDto() };
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns(dto);
            _antrianMapDetilDal
                .Setup(x => x.ListData(It.IsAny<IAntrianMapKey>()))
                .Returns(detilDtos);

            // Act
            var result = _sut.LoadEntity(key);

            // Assert
            result.HasValue.Should().BeTrue();
            result.Match(
                onSome: model =>
                {
                    model.AntrianMapId.Should().Be("AM01");
                    model.JadwalId.Should().Be("J01");
                    model.Dokter.PpaId.Should().Be("D01");
                    model.Layanan.LayananId.Should().Be("L01");
                    model.TotalSlotCount.Should().Be(1);
                },
                onNone: () => Assert.Fail("Expected Some but got None"));
        }

        [Fact]
        public void LoadEntity_ShouldReturnNone_WhenKeyNotFound()
        {
            // Arrange
            var key = BuildKey("AM-MISSING");
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns((AntrianMapDto)null!);

            // Act
            var result = _sut.LoadEntity(key);

            // Assert
            result.HasValue.Should().BeFalse();
            _antrianMapDetilDal.Verify(
                x => x.ListData(It.IsAny<IAntrianMapKey>()),
                Times.Never,
                "detil dal must not be queried when the header is not found");
        }

        [Fact]
        public void LoadEntity_ShouldReturnModelWithEmptyListMap_WhenDetilDalReturnsNull()
        {
            // Arrange
            var key = BuildKey();
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns(BuildDto());
            _antrianMapDetilDal
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
        public void LoadEntity_ShouldReturnModelWithEmptyListMap_WhenDetilDalReturnsEmptyList()
        {
            // Arrange
            var key = BuildKey();
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns(BuildDto());
            _antrianMapDetilDal
                .Setup(x => x.ListData(It.IsAny<IAntrianMapKey>()))
                .Returns([]);

            // Act
            var result = _sut.LoadEntity(key);

            // Assert
            result.HasValue.Should().BeTrue();
            result.Match(
                onSome: model => model.TotalSlotCount.Should().Be(0),
                onNone: () => Assert.Fail("Expected Some but got None"));
        }

        [Fact]
        public void LoadEntity_ShouldReturnModelWithCorrectSlotCount_WhenDetilDalReturnsMultipleItems()
        {
            // Arrange
            var key = BuildKey();
            var detilDtos = new List<AntrianMapDetilDto>
            {
                BuildDetilDto(noAntrian: 1, pasienName: "Pasien A", mr: "MR001"),
                BuildDetilDto(noAntrian: 2, pasienName: "Pasien B", mr: "MR002"),
                BuildDetilDto(noAntrian: 3, pasienName: "Pasien C", mr: "MR003"),
            };
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns(BuildDto());
            _antrianMapDetilDal
                .Setup(x => x.ListData(It.IsAny<IAntrianMapKey>()))
                .Returns(detilDtos);

            // Act
            var result = _sut.LoadEntity(key);

            // Assert
            result.HasValue.Should().BeTrue();
            result.Match(
                onSome: model => model.TotalSlotCount.Should().Be(3),
                onNone: () => Assert.Fail("Expected Some but got None"));
        }

        [Fact]
        public void LoadEntity_ShouldQueryDetilDalExactlyOnce_WhenKeyExists()
        {
            // Arrange
            var key = BuildKey();
            _antrianMapDal
                .Setup(x => x.GetData(It.IsAny<IAntrianMapKey>()))
                .Returns(BuildDto());
            _antrianMapDetilDal
                .Setup(x => x.ListData(It.IsAny<IAntrianMapKey>()))
                .Returns([]);

            // Act
            _sut.LoadEntity(key);

            // Assert
            _antrianMapDetilDal.Verify(
                x => x.ListData(It.IsAny<IAntrianMapKey>()),
                Times.Once);
        }
    }

    public class ListDataTests : AntrianMapRepoTest
    {
        [Fact]
        public void ListData_ShouldReturnMappedViews_WhenDalReturnsDtos()
        {
            // Arrange
            var lynKey = BuildLayananKey();
            var ppaKey = BuildPpaKey();
            var tgl    = new DateOnly(2026, 4, 22);
            var dtos   = new List<AntrianMapDto> { BuildDto(), BuildDto(id: "AM02", jadwalId: "J02") };
            _antrianMapDal
                .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), tgl))
                .Returns(dtos);

            // Act
            var result = _sut.ListData(lynKey, ppaKey, tgl).ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(v => v.JadwalId == "J01");
            result.Should().Contain(v => v.JadwalId == "J02");
        }

        [Fact]
        public void ListData_ShouldReturnEmpty_WhenDalReturnsNull()
        {
            // Arrange
            var lynKey = BuildLayananKey();
            var ppaKey = BuildPpaKey();
            var tgl    = new DateOnly(2026, 4, 22);
            _antrianMapDal
                .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), tgl))
                .Returns((IEnumerable<AntrianMapDto>)null!);

            // Act
            var result = _sut.ListData(lynKey, ppaKey, tgl).ToList();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ListData_ShouldReturnEmpty_WhenDalReturnsEmptyList()
        {
            // Arrange
            var lynKey = BuildLayananKey();
            var ppaKey = BuildPpaKey();
            var tgl    = new DateOnly(2026, 4, 22);
            _antrianMapDal
                .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), tgl))
                .Returns([]);

            // Act
            var result = _sut.ListData(lynKey, ppaKey, tgl).ToList();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ListData_ShouldMapDokterAndLayananFromDto_WhenDalReturnsSingleDto()
        {
            // Arrange
            var lynKey = BuildLayananKey();
            var ppaKey = BuildPpaKey();
            var tgl    = new DateOnly(2026, 4, 22);
            var dto    = BuildDto(dokterId: "D01", dokterName: "Dr. Satu", layananId: "L01", layananName: "Poli Umum");
            _antrianMapDal
                .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), tgl))
                .Returns(new List<AntrianMapDto> { dto });

            // Act
            var result = _sut.ListData(lynKey, ppaKey, tgl).ToList();

            // Assert
            result.Should().ContainSingle();
            var view = result.First();
            view.Dokter.PpaId.Should().Be("D01");
            view.Dokter.PpaName.Should().Be("Dr. Satu");
            view.Layanan.LayananId.Should().Be("L01");
            view.Layanan.LayananName.Should().Be("Poli Umum");
        }

        [Fact]
        public void ListData_ShouldMapTglAndJamFromDto_WhenDalReturnsSingleDto()
        {
            // Arrange
            var lynKey = BuildLayananKey();
            var ppaKey = BuildPpaKey();
            var tgl    = new DateOnly(2026, 4, 22);
            _antrianMapDal
                .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), tgl))
                .Returns(new List<AntrianMapDto> { BuildDto() });

            // Act
            var result = _sut.ListData(lynKey, ppaKey, tgl).ToList();

            // Assert
            result.Should().ContainSingle();
            var view = result.First();
            view.TglJadwal.Should().Be(new DateOnly(2026, 4, 22));
            view.JamJadwal.Should().Be(new TimeOnly(8, 0));
            view.JamPraktek.Should().Be(new TimeOnly(8, 30));
        }

        [Fact]
        public void ListData_ShouldPassCorrectParametersToDal_WhenCalled()
        {
            // Arrange
            var lynKey = BuildLayananKey("L99");
            var ppaKey = BuildPpaKey("D99");
            var tgl    = new DateOnly(2026, 6, 15);
            _antrianMapDal
                .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), tgl))
                .Returns([]);

            // Act
            _sut.ListData(lynKey, ppaKey, tgl);

            // Assert
            _antrianMapDal.Verify(
                x => x.ListData(
                    It.Is<ILayananKey>(k => k.LayananId == "L99"),
                    It.Is<IPpaKey>(k => k.PpaId == "D99"),
                    tgl),
                Times.Once);
        }
    }
}