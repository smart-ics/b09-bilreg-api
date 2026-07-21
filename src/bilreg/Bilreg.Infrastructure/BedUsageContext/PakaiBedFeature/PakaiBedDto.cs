using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.ValidationHelper;

// ReSharper disable InconsistentNaming

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public record PakaiBedDto(
    string fs_kd_trs,
    string fd_tgl_in,
    string fs_jam_in,
    string fs_kd_petugas,
    string fd_tgl_out,
    string fs_jam_out,
    string fs_kd_petugas_out,
    string fs_kd_reg,
    string fs_kd_layanan,
    string fs_kd_layanan_dk,
    string fs_kd_kamar_tipe,
    string fs_kd_bed,
    decimal fn_tarif,
    string fd_tgl_void,
    string fs_jam_void,
    string fs_kd_petugas_void,
    string fd_tgl_entry,
    string fs_jam_entry,
    string fs_kd_kelas,
    string fs_mr,
    string fs_nm_pasien,
    string fs_nm_layanan,
    string fs_nm_bed,
    bool fb_bed_aktif,
    string fs_nm_kamar_tipe,
    bool fb_kamar_tipe_aktif,
    string fs_nm_kelas)
{
    public static PakaiBedDto FromModel(PakaiBedModel model)
        => new(
            fs_kd_trs: model.PakaiBedId,
            fd_tgl_in: model.Periode.Masuk.Timestamp.ToString(DateFormatEnum.YMD),
            fs_jam_in: model.Periode.Masuk.Timestamp.ToString(DateFormatEnum.HMS),
            fs_kd_petugas: model.Periode.Masuk.UserId,
            fd_tgl_out: model.Periode.Keluar.Timestamp.ToString(DateFormatEnum.YMD),
            fs_jam_out: model.Periode.Keluar.Timestamp.ToString(DateFormatEnum.HMS),
            fs_kd_petugas_out: model.Periode.Keluar.UserId,
            fs_kd_reg: model.Reg.RegId,
            fs_kd_layanan: model.Layanan.LayananId,
            fs_kd_layanan_dk: string.Empty,
            fs_kd_kamar_tipe: model.TipeKamar.TipeKamarId,
            fs_kd_bed: model.Bed.BedId,
            fn_tarif: model.Tarif,
            fd_tgl_void: model.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.YMD),
            fs_jam_void: model.AuditTrail.Voided.Timestamp.ToString(DateFormatEnum.HMS),
            fs_kd_petugas_void: model.AuditTrail.Voided.UserId,
            fd_tgl_entry: model.AuditTrail.Created.Timestamp.ToString(DateFormatEnum.YMD),
            fs_jam_entry: model.AuditTrail.Created.Timestamp.ToString(DateFormatEnum.HMS),
            fs_kd_kelas: model.Kelas.KelasId,
            fs_mr: model.Reg.PasienId,
            fs_nm_pasien: model.Reg.PasienName,
            fs_nm_layanan: model.Layanan.LayananName,
            fs_nm_bed: model.Bed.BedName,
            fb_bed_aktif: model.Bed.IsAktif,
            fs_nm_kamar_tipe: model.TipeKamar.TipeKamarName,
            fb_kamar_tipe_aktif: model.TipeKamar.IsAktif,
            fs_nm_kelas: model.Kelas.KelasName);

    public PakaiBedModel ToModel()
    {
        var periode = new PeriodePakaiBedType(
            new AuditInfoType(fs_kd_petugas, fd_tgl_in, fs_jam_in),
            new AuditInfoType(fs_kd_petugas_out, fd_tgl_out, fs_jam_out));
        var reg = new RegReff(fs_kd_reg, fs_mr, fs_nm_pasien);
        var layanan = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var bed = new BedReff(fs_kd_bed, fs_nm_bed, fb_bed_aktif);
        var tipeKamar = new TipeKamarReff(
            fs_kd_kamar_tipe, fs_nm_kamar_tipe, fb_kamar_tipe_aktif);
        var kelas = new KelasReff(fs_kd_kelas, fs_nm_kelas);
        var auditTrail = new AuditTrailType(
            new AuditInfoType(string.Empty, fd_tgl_entry, fs_jam_entry),
            AuditInfoType.Default,
            new AuditInfoType(fs_kd_petugas_void, fd_tgl_void, fs_jam_void));

        return new PakaiBedModel(
            fs_kd_trs, periode, reg, layanan, bed, tipeKamar,
            kelas, fn_tarif, auditTrail);
    }
}
