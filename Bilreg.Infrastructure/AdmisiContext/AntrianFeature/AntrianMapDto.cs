using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.AdmisiContext.BookingFeature;
using System.Globalization;
using System.Text.Json;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianMapDto
{
    public AntrianMapDto(string fs_kd_antrian_map, 
        string fs_kd_jadwal, string fs_kd_dokter, string fs_kd_layanan, 
        DateTime fd_tgl_jadwal, string fs_jam_jadwal, 
        string fs_jam_praktek, string fs_pattern, int fn_max, 
        string fs_nm_dokter, string fs_nm_layanan)
    {
        this.fs_kd_antrian_map = fs_kd_antrian_map;

        this.fs_kd_jadwal = fs_kd_jadwal;
        this.fs_kd_dokter = fs_kd_dokter;
        this.fs_kd_layanan = fs_kd_layanan;
        this.fd_tgl_jadwal = fd_tgl_jadwal;
        this.fs_jam_jadwal = fs_jam_jadwal;
        
        this.fs_jam_praktek = fs_jam_praktek;
        this.fs_pattern = fs_pattern;
        this.fn_max = fn_max;
        
        this.fs_nm_dokter = fs_nm_dokter;
        this.fs_nm_layanan = fs_nm_layanan;
    }

    public string fs_kd_antrian_map { get; set; }

    public string fs_kd_jadwal { get; set; }
    public string fs_kd_dokter { get; set; }
    public string fs_kd_layanan { get; set; }
    public DateTime fd_tgl_jadwal { get; set; }
    public string fs_jam_jadwal { get; set; }

    public string fs_jam_praktek { get; set; }
    public string fs_pattern { get; set; }
    public int fn_max { get; set; }
    
    public string fs_nm_dokter { get; set; }
    public string fs_nm_layanan { get; set; }


    public static AntrianMapDto FromModel(AntrianMapModel model)
    {
        var antrianPatternStr = JsonSerializer.Serialize(model.AntrianPattern);
        var result = new AntrianMapDto(
            model.AntrianMapId,
            model.JadwalId,
            model.Dokter.PpaId,
            model.Layanan.LayananId,
            model.TglJadwal.ToDateTime(TimeOnly.MinValue),
            model.JamJadwal.ToString("HH:mm", CultureInfo.InvariantCulture),
            model.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture),
            antrianPatternStr,
            model.MaxPasien,
            model.Dokter.PpaName,
            model.Layanan.LayananName
        );

        return result;
    }
    public AntrianMapModel ToModel(IEnumerable<AntrianMapDetilModel> listDetil)
    {
        AntrianPatternType antrianPattern;
        if (string.IsNullOrWhiteSpace(fs_pattern))
        {
            antrianPattern = AntrianPatternType.Default;
        }
        else
        {
            try
            {
                var trimmed = fs_pattern.TrimStart();
                if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
                    antrianPattern = AntrianPatternFactory.FromStringJson(fs_pattern) ?? AntrianPatternType.Default;
                else
                    antrianPattern = new AntrianPatternType(fs_pattern, fn_max, 0, new List<AntrianPatternItemType>());
            }
            catch (System.Text.Json.JsonException)
            {
                // fallback to legacy simple pattern representation stored as plain string
                antrianPattern = new AntrianPatternType(fs_pattern, fn_max, 0, new List<AntrianPatternItemType>());
            }
        }
        return new AntrianMapModel(
            fs_kd_antrian_map,
            fs_kd_jadwal,
            new PpaReff(fs_kd_dokter, fs_nm_dokter),
            new LayananReff(fs_kd_layanan, fs_nm_layanan),
            DateOnly.FromDateTime(fd_tgl_jadwal),
            TimeOnly.ParseExact(fs_jam_jadwal, @"HH\:mm", CultureInfo.InvariantCulture),
            TimeOnly.ParseExact(fs_jam_praktek, @"HH\:mm", CultureInfo.InvariantCulture),
            antrianPattern,
            fn_max,
            listMap: listDetil
        );
    }

    public AntrianMapHdrView ToView()
    {
        var dokter = new PpaReff(fs_kd_dokter, fs_nm_dokter);
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new AntrianMapHdrView(fs_kd_antrian_map, fs_kd_jadwal, dokter, lyn,
            DateOnly.FromDateTime(fd_tgl_jadwal),
            TimeOnly.ParseExact(fs_jam_jadwal, @"HH\:mm", CultureInfo.InvariantCulture),
            TimeOnly.ParseExact(fs_jam_praktek, @"HH\:mm", CultureInfo.InvariantCulture));
        return result;
    }
}