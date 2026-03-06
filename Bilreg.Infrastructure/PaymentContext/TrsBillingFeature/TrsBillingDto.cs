using System.Globalization;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

// ReSharper disable  InconsistentNaming

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public record TrsBillingDto(
    string fs_kd_trs,
    int fn_modul,
    string fd_tgl_trs,
    string fs_jam_trs,
    string fd_tgl_jam_trs,

    string fs_kd_reg,
    string fs_kd_layanan,
    string fs_kd_kelas,
    string fs_kd_rekap_cetak,
    string fs_kd_petugas,
    
    decimal fn_sub_total,
    decimal fn_diskon,
    decimal fn_biaya,
    decimal fn_tax,
    decimal fn_total,

    string fs_keterangan,
    string fs_keterangan2,
    string fs_kd_ref_biaya,
    int fn_qty,
    string fs_kd_trs_main,
    
    string fs_mr,
    string fs_nm_pasien,
    string fs_nm_layanan,
    string fs_nm_kelas,
    string fs_nm_rekap_cetak)
{
    public static TrsBillingDto FromModel(TrsBillingType model)
    {
        return new TrsBillingDto(
            model.TrsBillingId,
            model.Modul,
            model.TglTrs.ToString(DateFormatEnum.YMD),
            model.TglTrs.ToString(DateFormatEnum.HMS),
            model.TglTrs.ToString(DateFormatEnum.YMD_HMS),

            model.Reg.RegId,
            model.Layanan.LayananId,
            model.Kelas.KelasId,
            model.RekapCetak.RekapCetakId,
            model.AuditInfo.UserId,
            
            model.SubTotal,
            model.Diskon,
            model.Biaya,
            model.Tax,
            model.Total,
            
            model.Keterangan.Keterangan,
            model.Keterangan.Keterangan2,
            model.Keterangan.RefBiaya,
            (int)model.Keterangan.Qty,
            model.Keterangan.TrsMainId,
            
            model.Reg.PasienId,
            model.Reg.PasienName,
            model.Layanan.LayananName,
            model.Kelas.KelasName,
            model.RekapCetak.RekapCetakName
        );
    }

    public TrsBillingType ToModel(IEnumerable<TrsBilling2Base> listTrsBilling2)
    {
        var reg = new RegReff(fs_kd_reg, fs_mr, fs_nm_pasien);
        var layanan = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var kelas = new KelasReff(fs_kd_kelas, fs_nm_kelas);
        var rekapCetak = new RekapCetakReff(fs_kd_rekap_cetak, fs_nm_rekap_cetak);
        var keterangan = new TrsBillKetType(
            fs_keterangan, fs_keterangan2, fs_kd_ref_biaya, 
            fn_qty, fs_kd_trs_main);
        var tglTrs = DateTime.ParseExact($"{fd_tgl_trs} {fs_jam_trs}","yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var auditinfo = new AuditInfoType(fs_kd_petugas, tglTrs);
        return new TrsBillingType(
            fs_kd_trs, (int)fn_modul, tglTrs,
            reg, layanan, kelas, auditinfo,
            fn_sub_total, fn_diskon, fn_tax, fn_biaya,
            rekapCetak, keterangan, listTrsBilling2
        );
    }
    public TrsBillingView ToView()
    {
        var reg = new RegReff(fs_kd_reg, fs_mr, fs_nm_pasien);
        var layanan = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var kelas = new KelasReff(fs_kd_kelas, fs_nm_kelas);
        var keterangan = new TrsBillKetType(
            fs_keterangan, fs_keterangan2, fs_kd_ref_biaya,
            fn_qty, fs_kd_trs_main);
        var tglTrs = DateTime.ParseExact($"{fd_tgl_trs} {fs_jam_trs}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        return new TrsBillingView(
            fs_kd_trs, tglTrs, reg, layanan, kelas, keterangan, fn_total);
    }
}