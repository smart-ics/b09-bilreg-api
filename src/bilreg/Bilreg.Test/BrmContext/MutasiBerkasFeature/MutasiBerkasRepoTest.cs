using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasRepoTest
{
    private readonly Mock<IMutasiBerkasDal> _mutasiDalMock = new();
    private readonly Mock<IVolumeBerkasDal> _volumeDalMock = new();
    private readonly MutasiBerkasRepo _sut;

    public MutasiBerkasRepoTest()
    {
        _sut = new MutasiBerkasRepo(_mutasiDalMock.Object, _volumeDalMock.Object);
    }

    private static MutasiBerkasModel CreateSampleModel(
        string mutasiId = "MK26100001",
        string trsBuatId = "RG26100001",
        string pasienId = "00-01-02-03",
        MutasiBerkasStatusEnum status = MutasiBerkasStatusEnum.Dispatched)
    {
        var jadwal = new JadwalPelayananType("POL01", "DR001", new DateOnly(2026, 10, 17), "10:00", "1");
        var volume = new VolumeBerkasType(1, 1, 0, 0);
        return MutasiBerkasModel.CreateKirim(
            mutasiId,
            trsBuatId,
            pasienId,
            new DateOnly(2026, 10, 17),
            new TimeOnly(10, 30, 0),
            "USER01",
            jadwal,
            volume);
    }

    private static MutasiBerkasDto CreateSampleDto(
        string mutasiId = "MK26100001",
        string trsBuatId = "RG26100001",
        string mrId = "00-01-02-03",
        int status = 1)
    {
        return new MutasiBerkasDto
        {
            FS_KD_MUTASI = mutasiId,
            FS_KD_TRS_BUAT = trsBuatId,
            FS_MR = mrId,
            FD_TGL_MUTASI = "2026-10-17",
            FS_JAM_MUTASI = "10:30:00",
            FS_KD_PETUGAS_MEDIS = "DR001",
            FS_KD_LAYANAN = "POL01",
            FD_TGL_PRAKTEK = "2026-10-17",
            FS_JAM_PRAKTEK = "10:00",
            FS_KD_SESION_POLI = "1",
            FN_STATUS = status,
            FS_KD_PETUGAS = "USER01",
            FN_CETAK = 0,
            FS_KD_PETUGAS_CETAK = string.Empty,
            fd_tgl_cetak = "3000-01-01",
            fs_jam_cetak = "00:00:00",
            fd_tgl_void = "3000-01-01",
            fs_jam_void = "00:00:00",
            fs_kd_petugas_void = string.Empty
        };
    }

    [Fact]
    public void SaveChanges_WhenMutasiDoesNotExist_ShouldInsertMutasiAndUpsertVolume()
    {
        // Arrange
        var model = CreateSampleModel();
        _mutasiDalMock.Setup(x => x.GetByKey(model.MutasiBerkasId)).Returns((MutasiBerkasDto?)null);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _mutasiDalMock.Verify(x => x.Insert(It.Is<MutasiBerkasDto>(d =>
            d.FS_KD_MUTASI == model.MutasiBerkasId &&
            d.FS_KD_TRS_BUAT == model.TrsBuatId &&
            d.FS_MR == model.PasienId)), Times.Once);

        _mutasiDalMock.Verify(x => x.Update(It.IsAny<MutasiBerkasDto>()), Times.Never);

        _volumeDalMock.Verify(x => x.Upsert(It.Is<VolumeBerkasDto>(v =>
            v.FS_KD_REG == model.TrsBuatId &&
            v.FS_MR == model.PasienId &&
            v.FN_QTY_BERKAS_PINJAM_RI == model.Volume.QtyPinjamRi &&
            v.FN_QTY_BERKAS_PINJAM_RJ == model.Volume.QtyPinjamRj)), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenMutasiAlreadyExists_ShouldUpdateMutasiAndUpsertVolume()
    {
        // Arrange
        var model = CreateSampleModel();
        var existingDto = CreateSampleDto(model.MutasiBerkasId, model.TrsBuatId, model.PasienId);
        _mutasiDalMock.Setup(x => x.GetByKey(model.MutasiBerkasId)).Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _mutasiDalMock.Verify(x => x.Update(It.Is<MutasiBerkasDto>(d =>
            d.FS_KD_MUTASI == model.MutasiBerkasId &&
            d.FS_KD_TRS_BUAT == model.TrsBuatId &&
            d.FS_MR == model.PasienId)), Times.Once);

        _mutasiDalMock.Verify(x => x.Insert(It.IsAny<MutasiBerkasDto>()), Times.Never);

        _volumeDalMock.Verify(x => x.Upsert(It.Is<VolumeBerkasDto>(v =>
            v.FS_KD_REG == model.TrsBuatId &&
            v.FS_MR == model.PasienId)), Times.Once);
    }

    [Fact]
    public void LoadEntity_WhenKeyIsNull_ShouldReturnNone()
    {
        // Act
        var result = _sut.LoadEntity(null!);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadEntity_WhenKeyIdIsEmpty_ShouldReturnNone()
    {
        // Act
        var result = _sut.LoadEntity(new MutasiBerkasKey(""));

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadEntity_WhenNotFoundInDal_ShouldReturnNone()
    {
        // Arrange
        _mutasiDalMock.Setup(x => x.GetByKey("MK26100001")).Returns((MutasiBerkasDto?)null);

        // Act
        var result = _sut.LoadEntity(new MutasiBerkasKey("MK26100001"));

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadEntity_WhenFoundWithVolume_ShouldReturnModelWithVolume()
    {
        // Arrange
        var dto = CreateSampleDto("MK26100001", "RG26100001", "00-01-02-03");
        var volumeDto = new VolumeBerkasDto
        {
            FS_KD_REG = "RG26100001",
            FS_MR = "00-01-02-03",
            FN_QTY_BERKAS_PINJAM_RI = 2,
            FN_QTY_BERKAS_PINJAM_RJ = 1,
            FN_QTY_BERKAS_KEMBALI_RI = 0,
            FN_QTY_BERKAS_KEMBALI_RJ = 0
        };

        _mutasiDalMock.Setup(x => x.GetByKey("MK26100001")).Returns(dto);
        _volumeDalMock.Setup(x => x.GetData("RG26100001")).Returns(volumeDto);

        // Act
        var result = _sut.LoadEntity(new MutasiBerkasKey("MK26100001"));

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.MutasiBerkasId.Should().Be("MK26100001");
        result.Value.TrsBuatId.Should().Be("RG26100001");
        result.Value.PasienId.Should().Be("00-01-02-03");
        result.Value.Volume.QtyPinjamRi.Should().Be(2);
        result.Value.Volume.QtyPinjamRj.Should().Be(1);
        result.Value.Volume.TotalPinjam.Should().Be(3);
    }

    [Fact]
    public void LoadEntity_WhenFoundWithoutVolume_ShouldReturnModelWithDefaultVolume()
    {
        // Arrange
        var dto = CreateSampleDto("MK26100001", "RG26100001", "00-01-02-03");
        _mutasiDalMock.Setup(x => x.GetByKey("MK26100001")).Returns(dto);
        _volumeDalMock.Setup(x => x.GetData("RG26100001")).Returns((VolumeBerkasDto?)null);

        // Act
        var result = _sut.LoadEntity(new MutasiBerkasKey("MK26100001"));

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Volume.Should().Be(VolumeBerkasType.Default);
    }

    [Fact]
    public void GetLatestActiveByMr_WhenMrIdIsEmpty_ShouldReturnNone()
    {
        // Act
        var result = _sut.GetLatestActiveByMr("");

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GetLatestActiveByMr_WhenNotFound_ShouldReturnNone()
    {
        // Arrange
        _mutasiDalMock.Setup(x => x.GetLatestActiveByMr("00-01-02-03")).Returns((MutasiBerkasDto?)null);

        // Act
        var result = _sut.GetLatestActiveByMr("00-01-02-03");

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GetLatestActiveByMr_WhenFound_ShouldReturnModelWithVolume()
    {
        // Arrange
        var dto = CreateSampleDto("MK26100001", "RG26100001", "00-01-02-03");
        var volumeDto = new VolumeBerkasDto
        {
            FS_KD_REG = "RG26100001",
            FS_MR = "00-01-02-03",
            FN_QTY_BERKAS_PINJAM_RI = 1,
            FN_QTY_BERKAS_PINJAM_RJ = 0
        };

        _mutasiDalMock.Setup(x => x.GetLatestActiveByMr("00-01-02-03")).Returns(dto);
        _volumeDalMock.Setup(x => x.GetData("RG26100001")).Returns(volumeDto);

        // Act
        var result = _sut.GetLatestActiveByMr("00-01-02-03");

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.MutasiBerkasId.Should().Be("MK26100001");
        result.Value.PasienId.Should().Be("00-01-02-03");
        result.Value.Volume.QtyPinjamRi.Should().Be(1);
    }

    [Fact]
    public void ListByTrsBuat_WhenTrsBuatIdIsEmpty_ShouldReturnEmpty()
    {
        // Act
        var result = _sut.ListByTrsBuat("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ListByTrsBuat_WhenNotFound_ShouldReturnEmpty()
    {
        // Arrange
        _mutasiDalMock.Setup(x => x.ListByTrsBuat("RG26100001")).Returns(new List<MutasiBerkasDto>());

        // Act
        var result = _sut.ListByTrsBuat("RG26100001");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ListByTrsBuat_WhenFound_ShouldReturnAllModelsWithVolume()
    {
        // Arrange
        var dto1 = CreateSampleDto("MK26100001", "RG26100001", "00-01-02-03", status: 1);
        var dto2 = CreateSampleDto("MP26100002", "RG26100001", "00-01-02-03", status: 2);
        var volumeDto = new VolumeBerkasDto
        {
            FS_KD_REG = "RG26100001",
            FS_MR = "00-01-02-03",
            FN_QTY_BERKAS_PINJAM_RI = 1,
            FN_QTY_BERKAS_PINJAM_RJ = 1
        };

        _mutasiDalMock.Setup(x => x.ListByTrsBuat("RG26100001")).Returns(new List<MutasiBerkasDto> { dto1, dto2 });
        _volumeDalMock.Setup(x => x.GetData("RG26100001")).Returns(volumeDto);

        // Act
        var result = _sut.ListByTrsBuat("RG26100001").ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].MutasiBerkasId.Should().Be("MK26100001");
        result[1].MutasiBerkasId.Should().Be("MP26100002");
        result[0].Volume.TotalPinjam.Should().Be(2);
        result[1].Volume.TotalPinjam.Should().Be(2);
    }
}
