using System.Text.Json;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class PolisViewDtoTest
{
    [Fact]
    public void UT01_GivenPolisViewDtoWithFullData_WhenToModel_ThenPolisViewPropertiesMatch()
    {
        // Arrange
        var dto = new PolisViewDto(
            fs_kd_polis: "POL001",
            fs_no_polis: "00012345678",
            fs_atas_nama: "John Doe",
            fs_kd_tipe_jaminan: "TJ01",
            fd_tgl_expired: "2026-12-31",
            fs_kd_kelas_ri: "401",
            fs_nm_kelas: "KELAS III",
            fs_mr: "PS00026500",
            fs_nm_pasien: "John Doe",
            fd_tgl_lahir: "1990-01-15",
            fs_jns_kelamin: "L",
            fs_nm_tipe_jaminan: "BPJS Kesehatan"
        );

        // Act
        var model = dto.ToModel();

        // Assert
        model.PolisId.Should().Be("POL001");
        model.NoPolis.Should().Be("00012345678");
        model.AtasName.Should().Be("John Doe");
        model.Pasien.PasienId.Should().Be("PS00026500");
        model.Pasien.PasienName.Should().Be("John Doe");
        model.Pasien.TglLahir.Should().Be(new DateOnly(1990, 1, 15));
        model.Pasien.Gender.Should().Be("L");
        model.TipeJaminan.TipeJaminanId.Should().Be("TJ01");
        model.TipeJaminan.TipeJaminanName.Should().Be("BPJS Kesehatan");
        model.Kelas.KelasId.Should().Be("401");
        model.Kelas.KelasName.Should().Be("KELAS III");
        model.TglExpired.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void UT02_GivenPolisViewDtoWithEmptyKelas_WhenToModel_ThenPolisViewHasDefaultKelasReff()
    {
        // Arrange
        var dto = new PolisViewDto(
            fs_kd_polis: "POL002",
            fs_no_polis: "00098765432",
            fs_atas_nama: "Jane Doe",
            fs_kd_tipe_jaminan: "TJ02",
            fd_tgl_expired: "2027-06-30",
            fs_kd_kelas_ri: "",
            fs_nm_kelas: "",
            fs_mr: "PS00026501",
            fs_nm_pasien: "Jane Doe",
            fd_tgl_lahir: "1995-05-20",
            fs_jns_kelamin: "P",
            fs_nm_tipe_jaminan: "Umum"
        );

        // Act
        var model = dto.ToModel();

        // Assert
        model.Kelas.Should().Be(KelasType.Default.ToReff());
        model.Kelas.KelasId.Should().Be("-");
        model.Kelas.KelasName.Should().Be("-");
    }

    [Fact]
    public void UT03_GivenPolisViewDtoWithNullKelas_WhenToModel_ThenPolisViewHasDefaultKelasReff()
    {
        // Arrange
        var dto = new PolisViewDto(
            fs_kd_polis: "POL003",
            fs_no_polis: "00011122233",
            fs_atas_nama: "Bob Smith",
            fs_kd_tipe_jaminan: "TJ01",
            fd_tgl_expired: "2028-01-01",
            fs_kd_kelas_ri: null!,
            fs_nm_kelas: null!,
            fs_mr: "PS00026502",
            fs_nm_pasien: "Bob Smith",
            fd_tgl_lahir: "1985-11-10",
            fs_jns_kelamin: "L",
            fs_nm_tipe_jaminan: "BPJS Kesehatan"
        );

        // Act
        var model = dto.ToModel();

        // Assert
        model.Kelas.Should().Be(KelasType.Default.ToReff());
        model.Kelas.KelasId.Should().Be("-");
        model.Kelas.KelasName.Should().Be("-");
    }

    [Fact]
    public void UT04_GivenPolisRepoListData_WhenPolisDalReturnsDtos_ThenReturnsPolisViewsWithKelas()
    {
        // Arrange
        var mockPolisDal = new Mock<IPolisDal>();
        var mockCoverDal = new Mock<IPolisCoverDal>();
        var pasienKey = PasienModel.Key("PS00026500");

        var dtos = new List<PolisViewDto>
        {
            new(
                fs_kd_polis: "POL001",
                fs_no_polis: "00012345678",
                fs_atas_nama: "John Doe",
                fs_kd_tipe_jaminan: "TJ01",
                fd_tgl_expired: "2026-12-31",
                fs_kd_kelas_ri: "401",
                fs_nm_kelas: "KELAS III",
                fs_mr: "PS00026500",
                fs_nm_pasien: "John Doe",
                fd_tgl_lahir: "1990-01-15",
                fs_jns_kelamin: "L",
                fs_nm_tipe_jaminan: "BPJS Kesehatan"
            )
        };

        mockPolisDal.Setup(x => x.ListData(pasienKey)).Returns(dtos);

        var repo = new PolisRepo(mockPolisDal.Object, mockCoverDal.Object);

        // Act
        var result = repo.ListData(pasienKey).ToList();

        // Assert
        result.Should().HaveCount(1);
        var first = result.First();
        first.PolisId.Should().Be("POL001");
        first.Kelas.KelasId.Should().Be("401");
        first.Kelas.KelasName.Should().Be("KELAS III");
    }

    [Fact]
    public async Task UT05_GivenPolisListHandler_WhenHandleCalled_ThenReturnsPolisViewsWithKelas()
    {
        // Arrange
        var mockRepo = new Mock<IPolisRepo>();
        var query = new PolisListQuery("PS00026500");

        var views = new List<PolisView>
        {
            new(
                "POL001", "00012345678", "John Doe",
                new PasienReff("PS00026500", "John Doe", new DateOnly(1990, 1, 15), "L"),
                new TipeJaminanReff("TJ01", "BPJS Kesehatan"),
                new KelasReff("401", "KELAS III"),
                new DateOnly(2026, 12, 31)
            )
        };

        mockRepo.Setup(x => x.ListData(query)).Returns(views);

        var handler = new PolisListHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Kelas.KelasId.Should().Be("401");
        result.First().Kelas.KelasName.Should().Be("KELAS III");
    }

    [Fact]
    public void UT06_GivenPolisView_WhenSerializedToJson_ThenMatchesCamelCaseContract()
    {
        // Arrange
        var view = new PolisView(
            "POL001", "00012345678", "John Doe",
            new PasienReff("PS00026500", "John Doe", new DateOnly(1990, 1, 15), "L"),
            new TipeJaminanReff("TJ01", "BPJS Kesehatan"),
            new KelasReff("401", "KELAS III"),
            new DateOnly(2026, 12, 31)
        );

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var jsonString = JsonSerializer.Serialize(view, jsonOptions);
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        // Assert
        root.TryGetProperty("kelas", out var kelasProp).Should().BeTrue();
        kelasProp.GetProperty("kelasId").GetString().Should().Be("401");
        kelasProp.GetProperty("kelasName").GetString().Should().Be("KELAS III");
        root.GetProperty("polisId").GetString().Should().Be("POL001");
        root.GetProperty("noPolis").GetString().Should().Be("00012345678");
        root.GetProperty("atasName").GetString().Should().Be("John Doe");
        root.GetProperty("pasien").GetProperty("pasienId").GetString().Should().Be("PS00026500");
        root.GetProperty("tipeJaminan").GetProperty("tipeJaminanId").GetString().Should().Be("TJ01");
    }
}
