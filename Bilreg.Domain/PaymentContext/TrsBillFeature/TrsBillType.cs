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
    private readonly List<TrsBill2FinalizationEventType> _listTrsBill2FinalizationEvent = [];
    private readonly List<TrsBill2PaymentEventType> _listTrsBill2PaymentEvent = [];

    #region CREATION

    public TrsBillType(string billingId, BillModulGroup modulGroup, DateTime tglTrs,
        RegReff reg, LayananReff layanan, KelasReff kelas,
        AuditInfoType auditInfo, RekapCetakReff rekapCetak,
        TrsBillNilaiType nilai,
        TrsBillKetType keterangan,
        IEnumerable<TrsBill2TransEventType> listTrsBilling2,
        IEnumerable<TrsBill2FinalizationEventType> listTrsBilling2FinalizationEvent,
        IEnumerable<TrsBill2PaymentEventType> listTrsBilling2PaymentEvent)
    {
        TrsBillingId = billingId;
        ModulGroup = modulGroup;
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
        _listTrsBill2FinalizationEvent = listTrsBilling2FinalizationEvent.ToList();
    }

    public static TrsBillType Default => new("-", BillModulGroup.Jasa, DateTime.MinValue,
        RegModel.Default.ToReff(), LayananType.Default.ToReff(),
        KelasType.Default.ToReff(),
        AuditInfoType.Default, RekapCetakType.Default.ToReff(),
        TrsBillNilaiType.Default,
        TrsBillKetType.Default, [], [], []);

    public static ITrsBillingKey Key(string id) => Default with { TrsBillingId = id };
    #endregion

    #region PROPERTIES

    public string TrsBillingId { get; init; }
    public BillModulGroup ModulGroup { get; init; }
    public DateTime TglTrs { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public KelasReff Kelas { get; init; }
    public AuditInfoType AuditInfo { get; init; }
    public RekapCetakReff RekapCetak { get; init; }
    public TrsBillNilaiType Nilai { get; init; }
    public TrsBillKetType Keterangan { get; init; }
    public IEnumerable<TrsBill2TransEventType> ListTransaction => _listTrsBill2TransEvent;
    public IEnumerable<TrsBill2FinalizationEventType> ListFinalization => _listTrsBill2FinalizationEvent;
    public IEnumerable<TrsBill2PaymentEventType> ListPayment => _listTrsBill2PaymentEvent;
    public TrsBillStatusEnum Status =>
        _listTrsBill2PaymentEvent.Count > 0 ? TrsBillStatusEnum.Paid
        : _listTrsBill2FinalizationEvent.Count > 0 ? TrsBillStatusEnum.Finalized
        : TrsBillStatusEnum.Transactioned;

    #endregion

    #region BEHAVIOR

    internal void FinalizeAllocation(
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
            throw new InvalidOperationException("Cannot finalize bill without transaction components.");

        var totalBase = _listTrsBill2TransEvent.Sum(x => x.Nilai);
        if (totalBase == 0)
            throw new InvalidOperationException("Cannot finalize bill when total component nilai is zero.");

        var totalFinalized = _listTrsBill2FinalizationEvent.Sum(x => x.Nilai);
        if (totalFinalized + nilai > totalBase)
            throw new InvalidOperationException("Finalization nilai exceeds remaining bill total.");

        var jenisBayar = ToJenisBayar(payment);
        var allocated = 0m;
        var noUrut = _listTrsBill2FinalizationEvent.Count;

        for (var i = 0; i < _listTrsBill2TransEvent.Count; i++)
        {
            var trans = _listTrsBill2TransEvent[i];
            var share = i == _listTrsBill2TransEvent.Count - 1
                ? nilai - allocated
                : nilai * trans.Nilai / totalBase;

            var finalization = TrsBill2FinalizationEventType.Create(
                noUrut++, trans.Komponen, jenisBayar, share,
                trans.PetugasMedis, petugasKasir, trsBayarId, tglBayar);

            _listTrsBill2FinalizationEvent.Add(finalization);
            allocated += share;
        }
    }

    internal void CancelFinalization()
    {
        if (_listTrsBill2PaymentEvent.Count > 0)
            throw new InvalidOperationException("Cannot cancel finalization with existing payments.");

        _listTrsBill2FinalizationEvent.Clear();
    }

    internal void Pay(PaymentType payment, decimal nilai, string trsBayarId, DateTime tglBayar)
    {
        if (nilai < 0)
            throw new ArgumentOutOfRangeException(nameof(nilai));

        if (string.IsNullOrWhiteSpace(trsBayarId))
            throw new ArgumentException("Trs bayar id should not be empty", nameof(trsBayarId));

        if (Status is not (TrsBillStatusEnum.Finalized or TrsBillStatusEnum.Paid))
            throw new InvalidOperationException("Cannot pay bill unless status is Finalized or Paid.");

        var matchingFinalization = _listTrsBill2FinalizationEvent
            .Where(d => JenisBayarMatchesPayment(d.JenisBayar, payment))
            .ToList();

        if (matchingFinalization.Count == 0)
            throw new InvalidOperationException(
                "Cannot pay bill without matching finalization components for the payment provider.");

        var totalBase = matchingFinalization.Sum(x => x.Nilai);
        if (totalBase == 0)
            throw new InvalidOperationException("Cannot pay bill when matching finalization nilai is zero.");

        var totalPaid = _listTrsBill2PaymentEvent
            .Where(p => JenisBayarMatchesPayment(p.JenisBayar, payment))
            .Sum(x => x.Nilai);

        if (totalPaid + nilai > totalBase)
            throw new InvalidOperationException("Payment nilai exceeds remaining finalized responsibility.");

        var allocated = 0m;
        var noUrut = _listTrsBill2PaymentEvent.Count;

        for (var i = 0; i < matchingFinalization.Count; i++)
        {
            var finalization = matchingFinalization[i];
            var share = i == matchingFinalization.Count - 1
                ? nilai - allocated
                : nilai * finalization.Nilai / totalBase;

            var paymentEvent = TrsBill2PaymentEventType.Create(
                noUrut++, finalization.Komponen, finalization.JenisBayar, payment, trsBayarId, share, tglBayar,
                finalization.PetugasMedis);

            _listTrsBill2PaymentEvent.Add(paymentEvent);
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
            $"Payment type '{payment.PaymentId}' is not supported for finalization.",
            nameof(payment));
    }

    private static bool JenisBayarMatchesPayment(TrsBillJenisBayarType jenisBayar, PaymentType payment)
    {
        var expected = ToJenisBayar(payment);

        if (jenisBayar.IsTipeJaminan && expected.IsTipeJaminan)
            return string.Equals(jenisBayar.JenisBayarId, expected.JenisBayarId, StringComparison.Ordinal);

        return jenisBayar == expected;
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
