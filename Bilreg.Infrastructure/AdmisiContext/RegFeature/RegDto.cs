using Bilreg.Domain.AdmisiContext.RegFeature;
using System.Globalization;

//  resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public record RegDto(
    string fs_kd_reg, 
    string fd_tgl_masuk, string fs_jam_masuk, string fs_kd_petugas,
    string fd_tgl_keluar,  string fs_jam_keluar, string fs_kd_petugas_keluar,
    string fd_tgl_cancel_out,  string fs_jam_cancel_out, string fs_kd_petugas_cancel_out,
    string fd_tgl_void, string fs_jam_void, string fs_kd_petugas_void,
    string fs_kd_jenis_reg, string fs_mr, string fs_kd_tipe_jaminan, string fs_kd_kelas, 
    string fs_kd_cara_masuk_dk,  string fs_kd_rujukan, string fs_kd_medis, 
    string fs_kd_layanan,  string fs_kd_karcis, string fs_no_sjp,
    string fd_tgl_jam_masuk, string fd_tgl_jam_keluar,
    //
    string fs_nm_pasien,  string fd_tgl_lahir, string fs_jns_kelamin,
    string fs_nm_tipe_jaminan, string fs_nm_kelas, string fs_nm_cara_masuk_dk, 
    string fs_nm_rujukan, string fs_nm_medis, string fs_nm_layanan, string fs_nm_karcis
    )
{
    public static RegDto FromModel(RegModel model)
    {
        var jenisRegStr = model.JenisReg.ToNumberString();
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
            model.SjpNo,
            model.RegMasukAudit.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            model.RegKeluarAudit.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
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

    // public RegModel ToModel(IEnumerable<RegKomponenType> listKomponen)
    // {
    //     var regMasukAudit = new AuditInfoType(fs_kd_petugas, fd_tgl_masuk, fs_jam_masuk);
    //     var regKeluarAudit = new AuditInfoType(fs_kd_petugas_keluar, fd_tgl_keluar, fs_jam_keluar);
    //     var regCancelOutAudit = new AuditInfoType(fs_kd_petugas_cancel_out, fd_tgl_cancel_out, fs_jam_cancel_out);
    //     var jenisReg = fs_kd_jenis_reg.ToJenisRegEnum();
    //     var pasien = new PasienReff(fs_mr, fs_nm_pasien, DateOnly.Parse(fd_tgl_lahir), fs_jns_kelamin);
    //     var tipeJmn = new TipeJaminanReff(fs_kd_tipe_jaminan, fs_nm_tipe_jaminan);
    //     var kelas = new KelasReff(fs_kd_kelas, fs_nm_kelas);
    //     var caraMasukDk = new CaraMasukDkType(fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk);
    //     var rujukan = new RujukanReff(fs_kd_rujukan, fs_nm_rujukan);
    //     var dokter = new PetugasMedisReff(fs_kd_medis, fs_nm_peg);
    //     var layanan = new LayananReff(fs_kd_layanan, fs_nm_layanan);
    //     var karcis = new KarcisReff(fs_kd_karcis, fs_nm_karcis);
    //     var result = new RegModel(
    //         fs_kd_reg, DateOnly.Parse(fd_tgl_masuk),
    //         regMasukAudit, regKeluarAudit, regCancelOutAudit, jenisReg,
    //         pasien, tipeJmn, polis, kelas, caraMasukDk, rujukan, dokter,
    //         layanan, karcis, listKomponen);
    //     return result;
    // }
}