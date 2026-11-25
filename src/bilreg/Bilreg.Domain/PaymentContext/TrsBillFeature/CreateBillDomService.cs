using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

public interface ICreateBillDomService
{
    TrsBillType FromReg(
        TataRekeningModel tataRekening,
        RegModel reg,
        KarcisType karcis,
        JaminanType jaminan,
        PpaType dokter,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default);

    TrsBillType FromTindakan(
        TataRekeningModel tataRekening,
        TindakanModel tindakan,
        RegModel reg,
        TarifType tarif,
        JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default);

    TrsBillType FromLabOrderItem(
        TataRekeningModel tataRekening,
        LabOrderModel labOrder, 
        LabOrderItemModel labOrderItem,
        RegModel reg,
        JaminanType jaminan,
        TarifType tarif,
        NilaiTarifType nilaiTarif,
        IEnumerable<KomponenType> listReffKomp,
        LayananReff? layanan = null,
        DateTime createdAt = default);

    TrsBillType FromRadOrderItem(
        TataRekeningModel tataRekening,
        RadOrderModel radOrder,
        RadOrderItemModel radOrderItem,
        RegModel reg,
        JaminanType jaminan,
        TarifType tarif,
        NilaiTarifType nilaiTarif,
        IEnumerable<KomponenType> listReffKomp,
        LayananReff? layanan = null,
        DateTime createdAt = default);

    TrsBillType FromPenjualanItem(
        TataRekeningModel tataRekening,
        PenjualanModel penjualan,
        PenjualanItemType item,
        RegModel reg,
        JaminanType jaminan,
        GroupRekType groupRek,
        DateTime createdAt = default);

    TrsBillType FromAmbulanceUsage(
        TataRekeningModel tataRekening,
        TrsTransportModel transport,
        RegModel reg,
        JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp,
        RekapCetakType? rekapCetak = null,
        DateTime createdAt = default);

    TrsBillType FromTindakanBarang(
        TataRekeningModel tataRekening,
        TindakanModel tindakan,
        TindakanBarangType barang,
        RegModel reg,
        JaminanType jaminan,
        GroupRekType groupRek,
        DateTime createdAt = default);

    TrsBillType FromBhpRanap(
        TataRekeningModel tataRekening,
        BhpRanapModel bhp,
        RegModel reg,
        LayananType layanan,
        JaminanType jaminan,
        GroupRekType groupRek,
        DateTime createdAt = default);
}

public sealed class CreateBillDomService : ICreateBillDomService
{
    public TrsBillType FromReg(
        TataRekeningModel tataRekening, RegModel reg, KarcisType karcis,
        JaminanType jaminan, PpaType dokter, IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromReg(reg, karcis, jaminan, dokter, listReffKomp, createdAt);
    }

    public TrsBillType FromTindakan(
        TataRekeningModel tataRekening, TindakanModel tindakan, RegModel reg,
        TarifType tarif, JaminanType jaminan, IEnumerable<KomponenType> listReffKomp,
        DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp, createdAt);
    }

    public TrsBillType FromTindakanBarang(
        TataRekeningModel tataRekening,
        TindakanModel tindakan,
        TindakanBarangType barang,
        RegModel reg,
        JaminanType jaminan,
        GroupRekType groupRek,
        DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromTindakanBarang(tindakan, barang, reg, jaminan, groupRek, createdAt);
    }

    public TrsBillType FromLabOrderItem(TataRekeningModel tataRekening, LabOrderModel labOrder, LabOrderItemModel labOrderItem, RegModel reg,
        JaminanType jaminan, TarifType tarif, NilaiTarifType nilaiTarif, IEnumerable<KomponenType> listReffKomp, 
        LayananReff? layanan = null, DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromLabOrderItem(labOrder, labOrderItem, reg, jaminan, tarif, nilaiTarif, listReffKomp, layanan, createdAt);
    }

    public TrsBillType FromRadOrderItem(TataRekeningModel tataRekening, RadOrderModel radOrder, RadOrderItemModel radOrderItem, RegModel reg,
        JaminanType jaminan, TarifType tarif, NilaiTarifType nilaiTarif, IEnumerable<KomponenType> listReffKomp, 
        LayananReff? layanan = null, DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromRadOrderItem(radOrder, radOrderItem, reg, jaminan, tarif, nilaiTarif, listReffKomp, layanan, createdAt);
    }

    public TrsBillType FromPenjualanItem(
        TataRekeningModel tataRekening,
        PenjualanModel penjualan,
        PenjualanItemType item,
        RegModel reg,
        JaminanType jaminan,
        GroupRekType groupRek,
        DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromPenjualanItem(penjualan, item, reg, jaminan, groupRek, createdAt);
    }

    public TrsBillType FromAmbulanceUsage(
        TataRekeningModel tataRekening,
        TrsTransportModel transport,
        RegModel reg,
        JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp,
        RekapCetakType? rekapCetak = null,
        DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromAmbulanceUsage(transport, reg, jaminan, listReffKomp, rekapCetak, createdAt);
    }

    public TrsBillType FromBhpRanap(
        TataRekeningModel tataRekening,
        BhpRanapModel bhp,
        RegModel reg,
        LayananType layanan,
        JaminanType jaminan,
        GroupRekType groupRek,
        DateTime createdAt = default)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromBhpRanap(bhp, reg, layanan, jaminan, groupRek, createdAt);
    }

    private static void ValidateReg(TataRekeningModel tataRekening, string regId)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);

        if (!string.Equals(tataRekening.RegId, regId, StringComparison.Ordinal))
            throw new ArgumentException(
                $"TataRekening registrasi '{tataRekening.RegId}' tidak sesuai dengan registrasi '{regId}'.",
                nameof(tataRekening));

        tataRekening.EnsureCanCreateTrsBill();
    }
}
