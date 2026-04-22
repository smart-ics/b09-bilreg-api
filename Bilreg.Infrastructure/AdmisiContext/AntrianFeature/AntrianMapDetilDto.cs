using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianMapDetilDto(
    string fs_kd_antrian_map, string fs_kd_dokter, string fs_kd_layanan,
    string fd_tgl_jadwal, string fs_jam_jadwal,
    decimal fn_no_antrian, string fs_flag,
    string fs_mr, string fs_nm_pasien,
    string fs_kd_trs_gen, bool fb_terpakai,
    string fs_nm_dokter, string fs_nm_layanan
)
{

    public static AntrianMapDetilDto FromModel(AntrianMapDetilModel model, AntrianMapModel hdr)
    {
        return new AntrianMapDetilDto(
            fs_kd_antrian_map: hdr.AntrianMapId,
            fs_kd_dokter: hdr.Dokter.PpaId,
            fs_kd_layanan: hdr.Layanan.LayananId,
            fd_tgl_jadwal: hdr.TglJadwal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            fs_jam_jadwal: hdr.JamJadwal.ToString("HH:mm", CultureInfo.InvariantCulture),
            fn_no_antrian: Convert.ToDecimal(model.NoUrut),
            fs_flag: model.Flag,
            fs_mr: model.Pasien.PasienId,
            fs_nm_pasien: model.Pasien.PasienName,
            fs_kd_trs_gen: model.ReffId,
            fb_terpakai: model.IsTerpakai,
            fs_nm_dokter: "-",
            fs_nm_layanan: "-"
        );
    }

    public AntrianMapDetilModel ToModel()
    {
        var pasien = new PasienReff(fs_mr, fs_nm_pasien, new DateOnly(3000,1,1), "-");
        var reg = new RegReff("-", fs_mr, fs_nm_pasien);
        var result = new AntrianMapDetilModel(
            (int)fn_no_antrian,
            pasien,
            reg,
            fs_kd_trs_gen,
            fs_flag, fb_terpakai);
            return result;
    }
}
