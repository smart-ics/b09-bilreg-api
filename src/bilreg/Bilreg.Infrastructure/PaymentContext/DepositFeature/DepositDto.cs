using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.DepositFeature;

public record DepositDto(
    string fs_kd_trs,
    string fd_tgl_trs,
    string fs_jam_trs,
    string fd_tgl_jam_trs,
    string fs_kd_petugas,
    string fs_kd_reg,
    string fs_kd_layanan,
    string fs_keterangan,
    string fd_tgl_void,
    string fs_jam_void,
    string fs_kd_petugas_void,
    string fs_kd_trs_gen,
    string fs_kd_trs_deposit_khusus,
    decimal fn_nilai_deposit,
    decimal fn_total_deposit,
    string crttgl,
    string crtjam,
    string crtusr,
    string updtgl,
    string updjam,
    string updusr,
    string fs_mr,
    string fs_nm_pasien,
    string fs_nm_layanan)
{
    private const string EmptyDate = "3000-01-01";
    private const string EmptyJam = "00:00:00";

    public static DepositDto FromModel(DepositModel model)
        => new(
            fs_kd_trs: model.DepositId,
            fd_tgl_trs: model.DepositDate.ToString(DateFormatEnum.YMD),
            fs_jam_trs: model.DepositDate.ToString(DateFormatEnum.HMS),
            fd_tgl_jam_trs: model.DepositDate.ToString(DateFormatEnum.YMD_HMS),
            fs_kd_petugas: model.Audit.Created.UserId,
            fs_kd_reg: model.Reg.RegId,
            fs_kd_layanan: model.Layanan.LayananId,
            fs_keterangan: model.Keterangan,
            fd_tgl_void: model.Audit.Voided.Timestamp.ToString(DateFormatEnum.YMD),
            fs_jam_void: model.Audit.Voided.Timestamp.ToString(DateFormatEnum.HMS),
            fs_kd_petugas_void: model.Audit.Voided.UserId,
            fs_kd_trs_gen: string.Empty,
            fs_kd_trs_deposit_khusus: string.Empty,
            fn_nilai_deposit: model.NilaiDeposit,
            fn_total_deposit: model.NilaiDeposit,
            crttgl: model.Audit.Created.Timestamp.ToString(DateFormatEnum.YMD),
            crtjam: model.Audit.Created.Timestamp.ToString(DateFormatEnum.HMS),
            crtusr: model.Audit.Created.UserId,
            updtgl: model.Audit.Modified.Timestamp.ToString(DateFormatEnum.YMD),
            updjam: model.Audit.Modified.Timestamp.ToString(DateFormatEnum.HMS),
            updusr: model.Audit.Modified.UserId,
            fs_mr: string.Empty,
            fs_nm_pasien: string.Empty,
            fs_nm_layanan: string.Empty);

    public DepositModel ToModel()
    {
        var auditTrail = new AuditTrailType(
            new AuditInfoType(crtusr, crttgl, crtjam),
            new AuditInfoType(updusr, updtgl, updjam),
            new AuditInfoType(fs_kd_petugas_void, fd_tgl_void, fs_jam_void));
        var tglJamVoid = $"{fd_tgl_void} {fs_jam_void}".Trim();
        if (fd_tgl_void != "3000-01-01" && !string.IsNullOrEmpty(tglJamVoid))
            auditTrail.Batal(fs_kd_petugas_void, DateTime.Parse(tglJamVoid));

        return new DepositModel(
            depositId: fs_kd_trs,
            depositDate: string.IsNullOrWhiteSpace(fd_tgl_jam_trs) ? default : DateTime.Parse(fd_tgl_jam_trs),
            reg: new RegReff(fs_kd_reg, fs_mr, fs_nm_pasien),
            layanan: new LayananReff(fs_kd_layanan, fs_nm_layanan),
            ketarangan: fs_keterangan,
            nilaiDeposit: fn_nilai_deposit,
            audit: auditTrail);
    }
}