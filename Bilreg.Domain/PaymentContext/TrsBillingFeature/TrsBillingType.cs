using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBillingType : ITrsBillingKey
{
    private readonly List<TrsBill2TransEventType> _listTrsBill2TransEvent = [];
    private readonly List<TrsBill2PaymentEventType> _listTrsBill2PaymentEvent = [];
    private readonly List<TrsBill2DischargeEventType> _listTrsBill2DischargeEvent = [];
    
    #region CREATION
    public TrsBillingType(string billingId, int modul, DateTime tglTrs, 
        RegReff reg, LayananReff layanan, KelasReff kelas, 
        AuditInfoType auditInfo, decimal subTotal, decimal diskon, 
        decimal tax, decimal biaya, RekapCetakReff rekapCetak, 
        TrsBillKetType keterangan, 
        IEnumerable<TrsBill2TransEventType> listTrsBilling2,
        IEnumerable<TrsBill2PaymentEventType> listTrsBilling2PaymentEvent,
        IEnumerable<TrsBill2DischargeEventType> listTrsBilling2DischargeEvent)
    {
        TrsBillingId = billingId;
        Modul = modul;
        TglTrs = tglTrs;
        Reg = reg;
        Layanan = layanan;
        Kelas = kelas;
        AuditInfo = auditInfo;
        SubTotal = subTotal;
        Diskon = diskon;
        Tax = tax;
        Biaya = biaya;
        RekapCetak = rekapCetak;
        Keterangan = keterangan;
        _listTrsBill2TransEvent = listTrsBilling2.ToList();
        _listTrsBill2PaymentEvent = listTrsBilling2PaymentEvent.ToList();
        _listTrsBill2DischargeEvent = listTrsBilling2DischargeEvent.ToList();
    }

    public static TrsBillingType CreateFromTindakan(
        TindakanModel tindakan,
        RegModel reg, TarifType tarif, JaminanType jaminan,
        IEnumerable<KomponenType> listReffKomp)
    {
        if (tarif.ToReff() != tindakan.Tarif)
            throw new ArgumentException("Tarif tidak sesuai");

        if (jaminan.JaminanId != reg.TipeJaminan.TipeJaminanId[..3])
            throw new ArgumentException("Jaminan tidak sesuai registrasi");

        var audit = AuditTrailType.Create(tindakan.AuditTrail.Created.UserId, DateTime.Now);
        var ketBilling = new TrsBillKetType(tarif.TarifName, "", tarif.TarifId, 1, "");
        var result = new TrsBillingType(tindakan.TindakanId, 0, tindakan.TindakanDate,
            tindakan.Reg, tindakan.Layanan, tindakan.Kelas, audit.Created, tindakan.Total, 0, 0, 0,
            tarif.RekapCetak, ketBilling, [], [], []);

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        var listReffKompFetched = listReffKomp.ToList();
        foreach (var item in tindakan.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);

            var rekPdpt = reffKomp?.RekPdpt?.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon?.CoaId ?? string.Empty;

            var ppa = item is TindakanKomponenWithPpaType kompWithPpa
                ? kompWithPpa.Ppa
                : PpaType.Default.ToReff();
            var bill2Coa = new TrsBill2CoaType(
                new CoaType(rekPpdp, " "),
                new CoaType(rekPdpt, ""),
                CoaType.Default,
                CoaType.Default,
                CoaType.Default,
                new CoaType(rekDiskon, ""));
            
            var komponen = new TrsBill2KomponenType(item.Komponen.KomponenId, item.Komponen.KomponenName);
            
            var trsBill2 = new TrsBill2TransEventType(
                i++,  komponen, TrsBill2JenisBayarType.Pdp, 
                item.Nilai, ppa, bill2Coa);

            result.AddTransactionEvent(trsBill2);
        }
        return result;
    }
    public static TrsBillingType CreateFromRegistrasi(RegModel reg,
        KarcisType karcis, JaminanType jaminan, PpaType dokter,
        IEnumerable<KomponenType> listReffKomp)
    {
        var audit = AuditTrailType.Create(reg.RegMasukAudit.UserId, DateTime.Now);
        var ketBilling = new TrsBillKetType($"REG : {karcis.KarcisName}", "", karcis.KarcisId, 1, "");
        var result = new TrsBillingType(reg.RegId, 0, reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
            reg.ToReff(), reg.Layanan, reg.Kelas, audit.Created, karcis.NilaiKarcis, reg.ListKomponen.Sum(x => x.Diskon), 0, 0,
            karcis.RekapCetak, ketBilling, []);

        var tglTrs = reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp));

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap.CoaId
            : jaminan.Rekening.PpdpJasaRajal.CoaId;

        var i = 0;
        var listReffKompFetched = listReffKomp.ToList();
        foreach (var item in reg.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);
            var rekPdpt = reffKomp?.RekPdpt?.CoaId ?? string.Empty;
            var rekDiskon = reffKomp?.RekDiskon?.CoaId ?? string.Empty;
            var rekJasa = new RekJasaType(rekPpdp, rekPdpt, rekDiskon);
            var ppa = reffKomp?.ListSatTugas?.Any() ?? false
                ? dokter.ToReff()
                : PpaType.Default.ToReff();
            var trsBill2 = new TrsBilling2JasaType(i++, reg.RegId, reg.RegDate.ToDateTime(TimeOnly.FromDateTime(reg.RegMasukAudit.Timestamp)),
                new NilaiBillingType("PDP", item.Nilai, 0), ppa, PegType.Default,
                item.Komponen, rekJasa);
            result.AddTransactionEvent(trsBill2);
        }
        return result;
    }

    public static TrsBillingType Default => new("-", 0, DateTime.MinValue, 
        RegModel.Default.ToReff(), LayananType.Default.ToReff(), 
        KelasType.Default.ToReff(), 
        AuditInfoType.Default, 0, 0, 0, 0, RekapCetakType.Default.ToReff(), 
        TrsBillKetType.Default, []);

    public static ITrsBillingKey Key(string id) => Default with { TrsBillingId = id };
    #endregion

    #region PROPERTIES
    public string TrsBillingId { get; init; }
    public int Modul { get; init; }
    public DateTime TglTrs { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public KelasReff Kelas { get; init; }
    public AuditInfoType AuditInfo { get; init; }
    public RekapCetakReff RekapCetak { get; init; }
    public decimal SubTotal { get; init; }
    public decimal Diskon { get; init; }
    public decimal Tax { get; init; }
    public decimal Biaya { get; init; }
    public decimal Total => SubTotal - Diskon + Tax + Biaya;
    public TrsBillKetType Keterangan { get; init; }
    public IEnumerable<TrsBill2TransEventType> ListBill2Transaction => _listTrsBill2TransEvent;
    public IEnumerable<TrsBill2DischargeEventType> ListBill2Discharge => _listTrsBill2DischargeEvent;
    public IEnumerable<TrsBill2PaymentEventType> ListBill2Payment => _listTrsBill2PaymentEvent;
    
    #endregion
    
    private void AddTransactionEvent(TrsBill2TransEventType trsBill2)
    {
        _listTrsBill2TransEvent.Add(trsBill2);
    }
}

public interface ITrsBillingKey
{
    string TrsBillingId { get; }
}

public record TrsBillKetType(string Keterangan, string Keterangan2, string RefBiaya, decimal Qty, string TrsMainId)
{
    public static TrsBillKetType Default => new("", "", "", 0, "");
}
