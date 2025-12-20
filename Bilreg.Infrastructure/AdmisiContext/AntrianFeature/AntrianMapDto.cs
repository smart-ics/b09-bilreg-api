using Bilreg.Domain.AdmisiContext.AntrianFeature;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianMapDto(
    string fs_kd_dokter, string fs_kd_layanan,
    string fd_tgl_jadwal, string fs_jam_jadwal,
    int fn_no_antrian, string fs_flag,
    string fs_mr, string fs_nm_pasien,
    string fs_kd_trs_gen,
    string fs_nm_dokter, string fs_nm_layanan
)
{

    public static AntrianMapDto FromModel(AntrianMapModel model)
    {
        return new AntrianMapDto(
            fs_kd_dokter: model.DokterId,
            fs_kd_layanan: model.LayananId,
            fd_tgl_jadwal: model.TglPraktek.ToString("yyyy-MM-dd"),
            fs_jam_jadwal: model.JamJadwal.ToString("HH:mm", CultureInfo.InvariantCulture),
            fn_no_antrian: model.NoUrut,
            fs_flag: model.Flag,
            fs_mr: model.Pasien.PasienId,
            fs_nm_pasien: model.Pasien.PasienName,
            fs_kd_trs_gen: model.ReffId,
            fs_nm_dokter: "-",
            fs_nm_layanan: "-"
        );
    }




}
