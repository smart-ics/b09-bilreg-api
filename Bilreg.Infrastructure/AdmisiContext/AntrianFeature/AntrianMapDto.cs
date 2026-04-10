using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.ValidationHelper;
using System.Collections.Immutable;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianMapDto(
    string fs_kd_dokter, string fs_kd_layanan,
    string fd_tgl_jadwal, string fs_jam_jadwal,
    decimal fn_no_antrian, string fs_flag,
    string fs_mr, string fs_nm_pasien,
    string fs_kd_trs_gen, bool fb_terpakai,
    string fs_nm_dokter, string fs_nm_layanan
    
)
{

    public static AntrianMapDto FromModel(AntrianMapModel model)
    {
        return new AntrianMapDto(
            fs_kd_dokter: model.DokterId,
            fs_kd_layanan: model.LayananId,
            fd_tgl_jadwal: model.TglPraktek.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            fs_jam_jadwal: model.JamJadwal.ToString("HH:mm", CultureInfo.InvariantCulture),
            fn_no_antrian: model.NoUrut,
            fs_flag: model.Flag,
            fs_mr: model.Pasien.PasienId,
            fs_nm_pasien: model.Pasien.PasienName,
            fs_kd_trs_gen: model.ReffId,
            fb_terpakai: model.IsTerpakai,
            fs_nm_dokter: "-",
            fs_nm_layanan: "-"
            
        );
    }

    public AntrianMapModel ToModel()
    {
        var pasien = new PasienReff(fs_mr, fs_nm_pasien, new DateOnly(3000,1,1), "-");
        var reg = new RegReff("-", fs_mr, fs_nm_pasien);
        var tgljadwal = DateOnly.ParseExact(fd_tgl_jadwal, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var jamJadwal = TimeOnly.ParseExact(fs_jam_jadwal, "HH:mm", CultureInfo.InvariantCulture);
        var result = new AntrianMapModel(
            tgljadwal,
            fs_kd_dokter,
            fs_kd_layanan,
            jamJadwal,
            (int)fn_no_antrian,
            pasien,
            reg,
            fs_kd_trs_gen,
            fs_flag, fb_terpakai);
            return result;
    }




}
