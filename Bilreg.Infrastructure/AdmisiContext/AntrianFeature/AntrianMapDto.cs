using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianMapDto
{
    public AntrianMapDto(string fs_kd_antrian_map, string fs_kd_jadwal, string fs_kd_dokter, 
        string fs_kd_layanan, DateTime fd_tgl_jadwal, string fs_jam_jadwal, string fs_jam_praktek, 
        string fs_nm_dokter, string fs_nm_layanan)
    {
        this.fs_kd_antrian_map = fs_kd_antrian_map;
        this.fs_kd_jadwal = fs_kd_jadwal;
        this.fs_kd_dokter = fs_kd_dokter;
        this.fs_kd_layanan = fs_kd_layanan;
        this.fd_tgl_jadwal = fd_tgl_jadwal;
        this.fs_jam_jadwal = fs_jam_jadwal;
        this.fs_jam_praktek = fs_jam_praktek;
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
    public string fs_nm_dokter { get; set; } 
    public string fs_nm_layanan { get; set; } 
    public string fs_pattern { get; set; }
    public int fn_max { get; set; }

    
    public static AntrianMapDto FromModel(AntrianMapModel model)
    {

        var result = new AntrianMapDto(
            fs_kd_antrian_map: model.AntrianMapId,
            fs_kd_jadwal: model.JadwalId,
            fs_kd_dokter: model.Dokter.PpaId,
            fs_kd_layanan: model.Layanan.LayananId,
            fd_tgl_jadwal: model.TglJadwal.ToDateTime(TimeOnly.MinValue),
            fs_jam_jadwal: model.JamJadwal.ToString("HH:mm", CultureInfo.InvariantCulture),
            fs_jam_praktek:model.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture),
            fs_nm_dokter: model.Dokter.PpaName,
            fs_nm_layanan: model.Layanan.LayananName
        );
        return result;
    }
    public AntrianMapModel ToModel(IEnumerable<AntrianMapDetilModel> listDetil)
    {
        return new AntrianMapModel(
            antrianMapId: fs_kd_antrian_map,
            jadwalId: fs_kd_jadwal,
            dokter: new PpaReff(fs_kd_dokter, fs_nm_dokter),
            layanan: new LayananReff(fs_kd_layanan, fs_nm_layanan),
            tglJadwal: DateOnly.FromDateTime(fd_tgl_jadwal),
            jamJadwal: TimeOnly.ParseExact(fs_jam_jadwal, @"HH\:mm", CultureInfo.InvariantCulture),
            jamPraktek: TimeOnly.ParseExact(fs_jam_praktek, @"HH\:mm", CultureInfo.InvariantCulture),
            pattern: fs_pattern,
            maxPasien: fn_max,
            listMap: listDetil
        );
    }

    public AntrianMapHdrView ToView()
    {
        var dokter = new PpaReff(fs_kd_dokter, fs_nm_dokter);
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new AntrianMapHdrView(fs_kd_jadwal, dokter, lyn,
            DateOnly.FromDateTime(fd_tgl_jadwal),
            TimeOnly.ParseExact(fs_jam_jadwal, @"HH\:mm", CultureInfo.InvariantCulture),
            TimeOnly.ParseExact(fs_jam_praktek, @"HH\:mm", CultureInfo.InvariantCulture));
        return result;
    }
}