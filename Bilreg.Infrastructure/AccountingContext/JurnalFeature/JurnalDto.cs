using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Infrastructure.AccountingContext.JurnalFeature;

public record JurnalDto(
    string fs_kd_jurnal,
    string fd_tgl_jurnal,
    string fs_jam_jurnal,
    string fs_kd_petugas,

    string fs_keterangan,
    string fs_no_bukti1,
    string fs_no_bukti2,
    string fs_no_bukti3,

    string fs_kd_reg,
    string fs_kd_mr,
    string fs_nm_pasien)
{
    public static JurnalDto FromModel(JurnalType model)
    {
        return new JurnalDto(
            model.JurnalId,
            model.TglJurnal.ToString(DateFormatEnum.YMD),
            model.TglJurnal.ToString(DateFormatEnum.HMS),
            model.AuditInfo.UserId,
    
            model.Keterangan.Keterangan,
            model.Keterangan.RefBukti1,
            model.Keterangan.RefBukti2,
            model.Keterangan.RefBukti3,

            model.Reg.RegId,
            model.Pasien.PasienId,
            model.Pasien.PasienName
        );
    }

    public JurnalType ToModel(IEnumerable<Jurnal2Base> listJurnal2)
    {
        var reg = new RegReff(fs_kd_reg, fs_kd_mr, fs_nm_pasien);
        var pasien = new PasienReff(fs_kd_mr, fs_nm_pasien, new DateOnly(3000, 1, 1), "-");
        var keterangan = new JurnalKetType(
            fs_keterangan, fs_no_bukti1, fs_no_bukti2, fs_no_bukti3);
        var tglJurnal = DateTime.ParseExact($"{fd_tgl_jurnal} {fs_jam_jurnal}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var auditinfo = new AuditInfoType(fs_kd_petugas, fd_tgl_jurnal, fs_jam_jurnal);

        return new JurnalType(
            fs_kd_jurnal, tglJurnal, auditinfo, keterangan,
            reg, pasien, listJurnal2
        );
    }
}
