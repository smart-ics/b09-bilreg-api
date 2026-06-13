using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBillType : ITrsBillingKey
{
    private readonly List<TrsBill2TransEventType> _listTrsBill2TransEvent = [];
    private readonly List<TrsBill2PaymentEventType> _listTrsBill2PaymentEvent = [];
    private readonly List<TrsBill2DischargeEventType> _listTrsBill2DischargeEvent = [];
    
    #region CREATION
    public TrsBillType(string billingId, int modul, DateTime tglTrs, 
        RegReff reg, LayananReff layanan, KelasReff kelas, 
        AuditInfoType auditInfo, RekapCetakReff rekapCetak,
        TrsBillNilaiType nilai, 
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
        RekapCetak = rekapCetak;
        Nilai = nilai;
        Keterangan = keterangan;
        _listTrsBill2TransEvent = listTrsBilling2.ToList();
        _listTrsBill2PaymentEvent = listTrsBilling2PaymentEvent.ToList();
        _listTrsBill2DischargeEvent = listTrsBilling2DischargeEvent.ToList();
    }

    public static TrsBillType Default => new("-", 0, DateTime.MinValue, 
        RegModel.Default.ToReff(), LayananType.Default.ToReff(), 
        KelasType.Default.ToReff(), 
        AuditInfoType.Default, RekapCetakType.Default.ToReff(),
        TrsBillNilaiType.Default,
        TrsBillKetType.Default, [],[],[]);

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
    public TrsBillNilaiType Nilai { get; init; }
    public TrsBillKetType Keterangan { get; init; }
    public IEnumerable<TrsBill2TransEventType> ListTransaction => _listTrsBill2TransEvent;
    public IEnumerable<TrsBill2DischargeEventType> ListDischarge => _listTrsBill2DischargeEvent;
    public IEnumerable<TrsBill2PaymentEventType> ListPayment => _listTrsBill2PaymentEvent;
    
    #endregion
    
    #region BEHAVIOR

    public void Discharge()
    {
        
    }
    #endregion
}

public interface ITrsBillingKey
{
    string TrsBillingId { get; }
}

public record TrsBillKetType(string Keterangan, string Keterangan2, string RefBiaya, decimal Qty, string TrsMainId)
{
    public static TrsBillKetType Default => new("", "", "", 0, "");
}
