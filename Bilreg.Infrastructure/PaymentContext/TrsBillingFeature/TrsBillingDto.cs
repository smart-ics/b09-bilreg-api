using System.Globalization;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

// ReSharper disable  InconsistentNaming

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public record TrsBillingDto(
    string fs_kd_trs,
    decimal fn_modul,
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
    decimal fn_qty,
    string fs_kd_trs_main,
    
    string fs_mr,
    string fs_nm_pasien,
    string fs_nm_layanan,
    string fs_nm_kelas,
    string fs_nm_rekap_cetak)
{
    public static TrsBillingDto FromModel(TrsBillType model)
    {
        return new TrsBillingDto(
            model.TrsBillingId,
            (int)model.ModulGroup,
            model.TglTrs.ToString(DateFormatEnum.YMD),
            model.TglTrs.ToString(DateFormatEnum.HMS),
            model.TglTrs.ToString(DateFormatEnum.YMD_HMS),

            model.Reg.RegId,
            model.Layanan.LayananId,
            model.Kelas.KelasId,
            model.RekapCetak.RekapCetakId,
            model.AuditInfo.UserId,
            
            model.Nilai.SubTotal,
            model.Nilai.Diskon,
            model.Nilai.Biaya,
            model.Nilai.Tax,
            model.Nilai.Total,
            
            model.Keterangan.Keterangan,
            model.Keterangan.Keterangan2,
            model.Keterangan.RefBiaya,
            model.Keterangan.Qty,
            model.Keterangan.TrsMainId,
            
            model.Reg.PasienId,
            model.Reg.PasienName,
            model.Layanan.LayananName,
            model.Kelas.KelasName,
            model.RekapCetak.RekapCetakName
        );
    }

    public TrsBillType ToModel(IEnumerable<ITrsBill2Event> listBill2Enum)
    {
        var listBill2 = listBill2Enum.ToList();
        var listTrans = listBill2.OfType<TrsBill2TransEventType>();
        var listDischarge = listBill2.OfType<TrsBill2DischargeEventType>();
        var listPayment = listBill2.OfType<TrsBill2PaymentEventType>();
        var reg = new RegReff(fs_kd_reg, fs_mr, fs_nm_pasien);
        var layanan = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var kelas = new KelasReff(fs_kd_kelas, fs_nm_kelas);
        var rekapCetak = new RekapCetakReff(fs_kd_rekap_cetak, fs_nm_rekap_cetak);
        var keterangan = new TrsBillKetType(
            fs_keterangan, fs_keterangan2, fs_kd_ref_biaya, 
            fn_qty, fs_kd_trs_main);
        var tglTrs = DateTime.ParseExact($"{fd_tgl_trs} {fs_jam_trs}","yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var auditinfo = new AuditInfoType(fs_kd_petugas, tglTrs);
        var modul = fn_modul == 0 ? BillModulGroup.Jasa : BillModulGroup.Obat;
        var nilai = new TrsBillNilaiType(fn_sub_total, fn_diskon, fn_tax, fn_biaya);
        return new TrsBillType(
            fs_kd_trs, modul, tglTrs,
            reg, layanan, kelas, auditinfo, rekapCetak,
            nilai, keterangan,
            listTrans, listDischarge, listPayment
        );
    }
    public TrsBillView ToView()
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
        var modul = fn_modul == 0 ? BillModulGroup.Jasa : BillModulGroup.Obat;
        var nilai = new TrsBillNilaiType(fn_sub_total, fn_diskon, fn_tax, fn_biaya);
        var result = new TrsBillView(
            fs_kd_trs, modul, tglTrs,
            reg, layanan, kelas, auditinfo, rekapCetak,
            nilai, keterangan);
        return result;
    }
    
}