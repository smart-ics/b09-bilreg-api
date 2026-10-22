using System.Globalization;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.DataTypeExtension;
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
    string fs_nm_rekap_cetak,

    // NEW FIELDS FOR TRACEABILITY (TD-003 / P3-S05)
    string SourceRegId = "",
    string SourceRegType = "",
    string ServiceUnitName = "",
    string BedCode = "",
    string CategoryCode = "",
    decimal? DifferentialAmount = null
)
{
    public static TrsBillingDto FromModel(TrsBillType model)
    {
        var ket = model.Keterangan.Keterangan ?? string.Empty;
        var keterangan = ket[..Math.Min(ket.Length, 40)];

        var ket2 = model.Keterangan.Keterangan2 ?? string.Empty;
        var keterangan2 = ket2[..Math.Min(ket2.Length, 40)];

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

            keterangan,
            keterangan2,
            model.Keterangan.RefBiaya,
            model.Keterangan.Qty,
            model.Keterangan.TrsMainId,

            model.Reg.PasienId,
            model.Reg.PasienName,
            model.Layanan.LayananName,
            model.Kelas.KelasName,
            model.RekapCetak.RekapCetakName
            // NEW FIELDS: initialized to empty/default via record default values (lines 48-53)
        );
    }

    public TrsBillType ToModel(IEnumerable<ITrsBill2Event> listBill2Enum)
    {
        var listBill2 = listBill2Enum.ToList();
        var listTrans = listBill2.OfType<TrsBill2TransEventType>();
        var listFinalization = listBill2.OfType<TrsBill2FinalizationEventType>();
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
            listTrans, listFinalization, listPayment
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
    
    // Mapping to the traceability DTO for P3-S05 / P4-S06
    public TrsBillingItemDto ToTrsBillingItemDto(
        string itemName,
        string categoryCode,
        string categoryName,
        string serviceUnitId,
        string sourceRegId,
        string sourceRegType,
        string bedCode,
        decimal? differentialAmount)
    {
        return new TrsBillingItemDto(
            BillingId: fs_kd_trs,
            ItemName: itemName,
            CategoryCode: categoryCode,
            CategoryName: categoryName,
            SourceRegId: sourceRegId,
            SourceRegType: sourceRegType,
            ServiceUnitId: serviceUnitId,
            ServiceUnitName: ServiceUnitName,
            BedCode: bedCode,
            TransactionDate: DateTime.ParseExact(fd_tgl_trs, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Qty: fn_qty,
            Tarif: fn_biaya / fn_qty,
            Diskon: fn_diskon,
            Total: fn_total,
            PhysicalClass: fs_nm_kelas,
            PayerClass: null,
            BillingClass: null,
            DifferentialAmount: differentialAmount
        );
    }
}