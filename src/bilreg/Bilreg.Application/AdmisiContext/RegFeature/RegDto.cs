using Bilreg.Domain.AdmisiContext.RegFeature;
using System.Globalization;

//  resharper disable inconsistentnaming
namespace Bilreg.Application.AdmisiContext.RegFeature;

public record RegDto(
    string fs_kd_reg, 
    string fd_tgl_masuk, string fs_jam_masuk, string fs_kd_petugas,
    string fd_tgl_keluar,  string fs_jam_keluar, string fs_kd_petugas_keluar,
    string fd_tgl_cancel_out,  string fs_jam_cancel_out, string fs_kd_petugas_cancel_out,
    string fd_tgl_void, string fs_jam_void, string fs_kd_petugas_void,
    string fs_kd_jenis_reg, string fs_mr, string fs_kd_tipe_jaminan, string fs_kd_kelas, 
    string fs_kd_cara_masuk_dk,  string fs_kd_rujukan, string fs_kd_medis, 
    string fs_kd_layanan,  string fs_kd_karcis, string fs_no_sjp, string fs_no_peserta, 
    string fs_kd_trs_sjp, string fd_tgl_jam_masuk, string fd_tgl_jam_keluar, decimal fn_karcis,
    string fs_kd_jenis_inap, string fs_reg_ibu,
    //
    string fs_nm_pasien,  string fd_tgl_lahir, string fs_jns_kelamin,
    string fs_nm_tipe_jaminan, string fs_nm_kelas, string fs_nm_cara_masuk_dk, 
    string fs_nm_rujukan, string fs_nm_medis, string fs_nm_layanan, string fs_nm_karcis
    )
{
    public static RegDto FromModel(RegModel model)
    {
        var jenisRegStr = model.JenisReg.ToNumberString();
        var jenisInapStr = model.JenisInap?.JenisInapId switch
        {
            null or "-" => string.Empty,
            var id => id
        };
        var regIbuStr = model.RegIbu ?? string.Empty;
        var result = new RegDto(
            model.RegId,
            model.RegDate.ToString("yyyy-MM-dd"),
            model.RegMasukAudit.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            model.RegMasukAudit.UserId,
            //
            model.RegKeluarAudit.Timestamp.ToString("yyyy-MM-dd"),
            model.RegKeluarAudit.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            model.RegKeluarAudit.UserId,
            //
            model.RegCancelOutAudit.Timestamp.ToString("yyyy-MM-dd"),
            model.RegCancelOutAudit.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            model.RegCancelOutAudit.UserId,
            //
            model.RegVoidAudit.Timestamp.ToString("yyyy-MM-dd"),
            model.RegVoidAudit.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            model.RegVoidAudit.UserId,

            //
            jenisRegStr,
            //
            model.Pasien.PasienId,
            model.TipeJaminan.TipeJaminanId,
            model.Kelas.KelasId,
            model.CaraMasukDk.CaraMasukDkId,
            model.Rujukan.RujukanId,
            model.Dokter.PpaId,
            model.Layanan.LayananId,
            model.Karcis.KarcisId,
            model.Eligibility.SjpNo,
            model.Eligibility.PesertaJaminanId,
            model.Eligibility.SjpId,
            model.RegMasukAudit.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            model.RegKeluarAudit.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            model.ListKomponen.Sum(k => k.Nilai),
            jenisInapStr,
            regIbuStr,
            //
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"),
            model.Pasien.Gender,
            model.TipeJaminan.TipeJaminanName,
            model.Kelas.KelasName,
            model.CaraMasukDk.CaraMasukDkName,
            model.Rujukan.RujukanName,
            model.Dokter.PpaName,
            model.Layanan.LayananName,
            model.Karcis.KarcisName
        );
        return result;
    }
}
