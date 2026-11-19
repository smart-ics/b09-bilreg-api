using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegRegAktifListQueryTest
{
    [Fact]
    public async Task Handle_GivenRegAktifModels_MapsAllFieldsIncludingTglLahirAndGenderCorrectly()
    {
        // Arrange
        var repoMock = new Mock<IRegAktifRepo>(MockBehavior.Strict);
        var models = new List<RegAktifModel>
        {
            BuildModel(
                regId: "RG20261119001",
                regDate: new DateOnly(2026, 11, 19),
                pasienId: "00012345",
                pasienName: "Budi Santoso",
                tglLahir: new DateOnly(1985, 5, 20),
                gender: "L",
                jenisReg: JenisRegEnum.RegJalan,
                layananId: "POLI01",
                layananName: "Poli Penyakit Dalam",
                dokterId: "D001",
                dokterName: "dr. Agus, Sp.PD",
                tipeJaminanId: "BPJS",
                tipeJaminanName: "BPJS Kesehatan",
                statusTataRekening: 0),
            BuildModel(
                regId: "RG20261119002",
                regDate: new DateOnly(2026, 11, 19),
                pasienId: "00067890",
                pasienName: "Siti Rahma",
                tglLahir: new DateOnly(1993, 12, 1),
                gender: "P",
                jenisReg: JenisRegEnum.Darurat,
                layananId: "IGD01",
                layananName: "Instalasi Gawat Darurat",
                dokterId: "D002",
                dokterName: "dr. Citra",
                tipeJaminanId: "UMUM",
                tipeJaminanName: "Umum / Pribadi",
                statusTataRekening: 1)
        };

        repoMock.Setup(x => x.ListData()).Returns(models);

        var sut = new RegRegAktifHandler(repoMock.Object);

        // Act
        var actual = (await sut.Handle(new RegRegAktifListQuery(), CancellationToken.None)).ToList();

        // Assert
        actual.Should().HaveCount(2);

        actual[0].Should().Be(new RegRegAktifListResponse(
            RegId: "RG20261119001",
            RegDate: "2026-11-19",
            PasienId: "00012345",
            PasienName: "Budi Santoso",
            JenisReg: "0",
            JenisRegString: "RegJalan",
            LayananId: "POLI01",
            LayananName: "Poli Penyakit Dalam",
            DokterId: "D001",
            DokterName: "dr. Agus, Sp.PD",
            StatusTataRekening: 0,
            TglLahir: "1985-05-20",
            Gender: "L"));

        actual[1].Should().Be(new RegRegAktifListResponse(
            RegId: "RG20261119002",
            RegDate: "2026-11-19",
            PasienId: "00067890",
            PasienName: "Siti Rahma",
            JenisReg: "3",
            JenisRegString: "Darurat",
            LayananId: "IGD01",
            LayananName: "Instalasi Gawat Darurat",
            DokterId: "D002",
            DokterName: "dr. Citra",
            StatusTataRekening: 1,
            TglLahir: "1993-12-01",
            Gender: "P"));

        repoMock.Verify(x => x.ListData(), Times.Once);
        repoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenRepoReturnsNull_ReturnsEmptyCollection()
    {
        // Arrange
        var repoMock = new Mock<IRegAktifRepo>(MockBehavior.Strict);
        repoMock.Setup(x => x.ListData()).Returns((IEnumerable<RegAktifModel>)null!);

        var sut = new RegRegAktifHandler(repoMock.Object);

        // Act
        var actual = await sut.Handle(new RegRegAktifListQuery(), CancellationToken.None);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
        repoMock.Verify(x => x.ListData(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepoReturnsEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        var repoMock = new Mock<IRegAktifRepo>(MockBehavior.Strict);
        repoMock.Setup(x => x.ListData()).Returns([]);

        var sut = new RegRegAktifHandler(repoMock.Object);

        // Act
        var actual = await sut.Handle(new RegRegAktifListQuery(), CancellationToken.None);

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
        repoMock.Verify(x => x.ListData(), Times.Once);
    }

    [Fact]
    public void Response_DefaultValues_SupportBackwardCompatibility()
    {
        // Act
        var response = new RegRegAktifListResponse(
            "RG01", "2026-11-19", "P01", "Pasien A",
            "0", "RegJalan", "L01", "Poli", "D01", "Dokter A");

        // Assert
        response.StatusTataRekening.Should().Be(0);
        response.TglLahir.Should().Be(string.Empty);
        response.Gender.Should().Be(string.Empty);
    }

    private static RegAktifModel BuildModel(
        string regId,
        DateOnly regDate,
        string pasienId,
        string pasienName,
        DateOnly tglLahir,
        string gender,
        JenisRegEnum jenisReg,
        string layananId,
        string layananName,
        string dokterId,
        string dokterName,
        string tipeJaminanId,
        string tipeJaminanName,
        int statusTataRekening) =>
        new(
            regId,
            regDate,
            new PasienReff(pasienId, pasienName, tglLahir, gender),
            jenisReg,
            new LayananReff(layananId, layananName),
            new PpaReff(dokterId, dokterName),
            new TipeJaminanReff(tipeJaminanId, tipeJaminanName),
            statusTataRekening);
}
