using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

public record TrsBillType : ITrsBillingKey
{
    private readonly List<TrsBill2TransEventType> _listTrsBill2TransEvent = [];
    private readonly List<TrsBill2DischargeEventType> _listTrsBill2DischargeEvent = [];
    private readonly List<TrsBill2PaymentEventType> _listTrsBill2PaymentEvent = [];
    
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
    public TrsBillStatusEnum Status =>
        _listTrsBill2PaymentEvent.Count > 0 ? TrsBillStatusEnum.Paid
        : _listTrsBill2DischargeEvent.Count > 0 ? TrsBillStatusEnum.Discharged
        : TrsBillStatusEnum.Transactioned;
    
    #endregion
    
    #region BEHAVIOR

    public void Discharge(
        PaymentType payment,
        decimal nilai,
        string petugasKasir,
        string trsBayarId,
        DateTime tglBayar)
    {
        if (nilai < 0)
            throw new ArgumentOutOfRangeException(nameof(nilai));

        if (string.IsNullOrWhiteSpace(petugasKasir))
            throw new ArgumentException("Petugas kasir should not be empty", nameof(petugasKasir));

        if (_listTrsBill2TransEvent.Count == 0)
            throw new InvalidOperationException("Cannot discharge bill without transaction components.");

        var totalBase = _listTrsBill2TransEvent.Sum(x => x.Nilai);
        if (totalBase == 0)
            throw new InvalidOperationException("Cannot discharge bill when total component nilai is zero.");

        var totalDischarged = _listTrsBill2DischargeEvent.Sum(x => x.Nilai);
        if (totalDischarged + nilai > totalBase)
            return;

        var jenisBayar = ToJenisBayar(payment);
        var allocated = 0m;
        var noUrut = _listTrsBill2DischargeEvent.Count;

        for (var i = 0; i < _listTrsBill2TransEvent.Count; i++)
        {
            var trans = _listTrsBill2TransEvent[i];
            var share = i == _listTrsBill2TransEvent.Count - 1
                ? nilai - allocated
                : nilai * trans.Nilai / totalBase;

            var discharge = TrsBill2DischargeEventType.Create(
                noUrut++, trans.Komponen, jenisBayar, share,
                trans.PetugasMedis, petugasKasir, trsBayarId, tglBayar);

            _listTrsBill2DischargeEvent.Add(discharge);
            allocated += share;
        }
    }

    private static TrsBillJenisBayarType ToJenisBayar(PaymentType payment)
    {
        if (payment == PaymentType.ByKas)
            return TrsBillJenisBayarType.Kas;

        if (payment == PaymentType.ByPri)
            return TrsBillJenisBayarType.Hut;

        if (payment.IsTipeJaminan)
            return new TrsBillJenisBayarType(payment.PaymentId, payment.PaymentName, true);

        throw new ArgumentException(
            $"Payment type '{payment.PaymentId}' is not supported for discharge.",
            nameof(payment));
    }

    public void CancelDischarge()
    {
        if (_listTrsBill2PaymentEvent.Count > 0)
            throw new InvalidOperationException("Cannot cancel discharge with existing payments.");

        _listTrsBill2DischargeEvent.Clear();
    }

    public void Pay()
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
