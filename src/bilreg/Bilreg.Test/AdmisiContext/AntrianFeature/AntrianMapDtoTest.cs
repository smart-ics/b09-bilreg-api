using System.Globalization;
using FluentAssertions;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapDtoTest
{
    [Fact]
    public void FromModel_MapsAllFields()
    {
        var dokter = new PpaReff("D001", "Dr. Example");
        var layanan = new LayananReff("L001", "Layanan Example");
        var tgl = DateOnly.FromDateTime(new DateTime(2026, 4, 22));
        var jamJadwal = TimeOnly.ParseExact("09:30", "HH:mm", CultureInfo.InvariantCulture);
        var jamPraktek = TimeOnly.ParseExact("10:00", "HH:mm", CultureInfo.InvariantCulture);
        var model = new AntrianMapModel("AM01", "J001", dokter, layanan, tgl, jamJadwal, jamPraktek, 
            AntrianPatternType.Default, 5, new List<AntrianMapDetilModel>());

        var dto = AntrianMapDto.FromModel(model);

        dto.fs_kd_antrian_map.Should().Be(model.AntrianMapId);
        dto.fs_kd_jadwal.Should().Be(model.JadwalId);
        dto.fs_kd_dokter.Should().Be(model.Dokter.PpaId);
        dto.fs_nm_dokter.Should().Be(model.Dokter.PpaName);
        dto.fs_kd_layanan.Should().Be(model.Layanan.LayananId);
        dto.fs_nm_layanan.Should().Be(model.Layanan.LayananName);
        dto.fd_tgl_jadwal.Should().Be(model.TglJadwal.ToDateTime(TimeOnly.MinValue));
        dto.fs_jam_jadwal.Should().Be(model.JamJadwal.ToString("HH:mm", CultureInfo.InvariantCulture));
        dto.fs_jam_praktek.Should().Be(model.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToModel_MapsAllFields()
    {
        var dto = new AntrianMapDto("AM02", "J002", null, "D002", "L002", new DateTime(2026, 4, 23), 
            "08:15", "08:45", "PATTRN", 30, "Dr Two", "Layanan Two");
        dto.fs_pattern = "P1";
        dto.fn_max = 10;
        var detils = new List<AntrianMapDetilModel>();

        var model = dto.ToModel(detils);

        model.AntrianMapId.Should().Be(dto.fs_kd_antrian_map);
        model.JadwalId.Should().Be(dto.fs_kd_jadwal);
        model.Dokter.PpaId.Should().Be(dto.fs_kd_dokter);
        model.Dokter.PpaName.Should().Be(dto.fs_nm_dokter);
        model.Layanan.LayananId.Should().Be(dto.fs_kd_layanan);
        model.Layanan.LayananName.Should().Be(dto.fs_nm_layanan);
        model.TglJadwal.Should().Be(DateOnly.FromDateTime(dto.fd_tgl_jadwal));
        model.JamJadwal.Should().Be(TimeOnly.ParseExact(dto.fs_jam_jadwal, @"HH\:mm", CultureInfo.InvariantCulture));
        model.JamPraktek.Should().Be(TimeOnly.ParseExact(dto.fs_jam_praktek, @"HH\:mm", CultureInfo.InvariantCulture));
        model.AntrianPattern.Tipe.Should().Be(dto.fs_pattern);
        model.MaxPasien.Should().Be(dto.fn_max);
        model.ListMap.Should().BeEquivalentTo(detils);
    }

    [Fact]
    public void ToView_ParsesDateAndTimes()
    {
        var dto = new AntrianMapDto("AM03", "J003", null, "D003", "L003",
            new DateTime(2026, 4, 24), "07:00", "07:30", 
            "PPPPP", 30, "Dr Three", "Layanan Three");

        var view = dto.ToView();

        view.JadwalId.Should().Be(dto.fs_kd_jadwal);
        view.Dokter.PpaId.Should().Be(dto.fs_kd_dokter);
        view.Dokter.PpaName.Should().Be(dto.fs_nm_dokter);
        view.Layanan.LayananId.Should().Be(dto.fs_kd_layanan);
        view.Layanan.LayananName.Should().Be(dto.fs_nm_layanan);
        view.TglJadwal.Should().Be(DateOnly.FromDateTime(dto.fd_tgl_jadwal));
        view.JamJadwal.Should().Be(TimeOnly.ParseExact(dto.fs_jam_jadwal, @"HH\:mm", CultureInfo.InvariantCulture));
        view.JamPraktek.Should().Be(TimeOnly.ParseExact(dto.fs_jam_praktek, @"HH\:mm", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RoundTrip_Dto_Model_Dto_PreservesKeyFields()
    {
        var dto = new AntrianMapDto("AM04", "J004", null, "D004", "L004", new DateTime(2026, 4, 25),
            "11:11", "11:30", "CCCC", 30, "Dr Four", "Layanan Four");
        var model = dto.ToModel(new List<AntrianMapDetilModel>());

        var dto2 = AntrianMapDto.FromModel(model);

        dto2.fs_kd_antrian_map.Should().Be(dto.fs_kd_antrian_map);
        dto2.fs_kd_jadwal.Should().Be(dto.fs_kd_jadwal);
        dto2.fs_kd_dokter.Should().Be(dto.fs_kd_dokter);
        dto2.fs_nm_dokter.Should().Be(dto.fs_nm_dokter);
        dto2.fs_kd_layanan.Should().Be(dto.fs_kd_layanan);
        dto2.fs_nm_layanan.Should().Be(dto.fs_nm_layanan);
        dto2.fs_jam_jadwal.Should().Be(dto.fs_jam_jadwal);
        dto2.fs_jam_praktek.Should().Be(dto.fs_jam_praktek);
    }
}