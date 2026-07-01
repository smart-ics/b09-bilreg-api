using System.Globalization;
using FluentAssertions;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapDetilDtoTest
{
    [Fact]
    public void FromModel_ShouldReturnDtoWithFormattedFields_WhenGivenExplicitModelAndHeader()
    {
        // Arrange
        var pasien = new PasienReff("P001", "John Doe", new DateOnly(1990, 1, 1), "M");
        var detil = new AntrianMapDetilModel(3, pasien.PasienName, pasien.PasienId, "REF123", "FLAG1", true);

        var dokter = new PpaReff("D01", "Dr. Who");
        var layanan = new LayananReff("L01", "Layanan A");
        var tgl = DateOnly.FromDateTime(new DateTime(2026, 4, 22));
        var jam = TimeOnly.ParseExact("09:30", "HH:mm", CultureInfo.InvariantCulture);
        var hdr = new AntrianMapModel("AM01", "J01", dokter, layanan, tgl, jam, 
            TimeOnly.ParseExact("10:00", "HH:mm", CultureInfo.InvariantCulture), AntrianPatternType.Default, 
            10, new List<AntrianMapDetilModel> { detil });

        // Act
        var dto = AntrianMapDetilDto.FromModel(detil, hdr);

        // Assert
        dto.fs_kd_antrian_map.Should().Be(hdr.AntrianMapId);
        dto.fs_kd_dokter.Should().Be(hdr.Dokter.PpaId);
        dto.fs_kd_layanan.Should().Be(hdr.Layanan.LayananId);
        dto.fd_tgl_jadwal.Should().Be(hdr.TglJadwal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        dto.fs_jam_jadwal.Should().Be(hdr.JamJadwal.ToString("HH:mm", CultureInfo.InvariantCulture));
        dto.fn_no_antrian.Should().Be((decimal)detil.NoUrut);
        dto.fs_flag.Should().Be(detil.Flag);
        dto.fs_mr.Should().Be(pasien.PasienId);
        dto.fs_nm_pasien.Should().Be(pasien.PasienName);
        dto.fs_kd_trs_gen.Should().Be(detil.ReffId);
        dto.fb_terpakai.Should().Be(detil.IsTerpakai);
        dto.fs_nm_dokter.Should().Be("-");
        dto.fs_nm_layanan.Should().Be("-");
    }

    [Fact]
    public void ToModel_ShouldReturnDetilModelWithConvertedTypes_WhenGivenExplicitDto()
    {
        // Arrange
        var dto = new AntrianMapDetilDto(
            fs_kd_antrian_map: "AM02",
            fs_kd_dokter: "D02",
            fs_kd_layanan: "L02",
            fd_tgl_jadwal: "2026-04-23",
            fs_jam_jadwal: "08:15",
            fn_no_antrian: 5m,
            fs_flag: "FLAGX",
            fs_mr: "P002",
            fs_nm_pasien: "Jane Doe",
            fs_kd_trs_gen: "REF5",
            fb_terpakai: false,
            fs_nm_dokter: "Dr Two",
            fs_nm_layanan: "Layanan Two"
        );

        // Act
        var model = dto.ToModel();

        // Assert
        model.NoUrut.Should().Be((int)dto.fn_no_antrian);
        model.PasienId.Should().Be(dto.fs_mr);
        model.PasienName.Should().Be(dto.fs_nm_pasien);
        model.ReffId.Should().Be(dto.fs_kd_trs_gen);
        model.Flag.Should().Be(dto.fs_flag);
        model.IsTerpakai.Should().Be(dto.fb_terpakai);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveKeyFields_WhenConvertModelToDtoAndBack()
    {
        // Arrange
        var pasien = new PasienReff("P010", "Alice", new DateOnly(1985, 6, 1), "F");
        var detil = new AntrianMapDetilModel(7, pasien.PasienName, pasien.PasienId, "REF7", "FLAG7", true);

        var dokter = new PpaReff("D10", "Dr Ten");
        var layanan = new LayananReff("L10", "Layanan Ten");
        var hdr = new AntrianMapModel("AM10", "J10", dokter, layanan, DateOnly.FromDateTime(new DateTime(2026, 4, 25)), 
            TimeOnly.ParseExact("11:11", "HH:mm", CultureInfo.InvariantCulture), 
            TimeOnly.ParseExact("11:30", "HH:mm", CultureInfo.InvariantCulture), 
            AntrianPatternType.Default, 5, new List<AntrianMapDetilModel> { detil });

        // Act
        var dto = AntrianMapDetilDto.FromModel(detil, hdr);
        var model2 = dto.ToModel();

        // Assert
        model2.NoUrut.Should().Be(detil.NoUrut);
        model2.PasienId.Should().Be(detil.PasienId);
        model2.PasienName.Should().Be(detil.PasienName);
        model2.ReffId.Should().Be(detil.ReffId);
        model2.Flag.Should().Be(detil.Flag);
        model2.IsTerpakai.Should().Be(detil.IsTerpakai);
    }
}