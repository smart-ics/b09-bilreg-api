using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

/// <summary>
/// Finalization component — financial responsibility allocation at charge level.
/// </summary>
public record TrsBill2FinalizationEventType : ITrsBill2Event
{
    public TrsBill2FinalizationEventType(int noUrut,
        TrsBill2KomponenType komponen,
        TrsBillJenisBayarType jenisBayar,
        decimal nilai,
        PpaReff petugasMedis,
        string petugasKasir,
        string trsBayarId,
        DateTime tglBayar)
    {
        NoUrut = noUrut;
        Komponen = komponen;
        JenisBayar = jenisBayar;
        Nilai = nilai;
        PetugasMedis = petugasMedis;
        PetugasKasir = petugasKasir;
        TrsBayarId = trsBayarId;
        TglBayar = tglBayar;
    }

    public static TrsBill2FinalizationEventType Default
        => new(0, TrsBill2KomponenType.Default, TrsBillJenisBayarType.Default, 0,
            PpaType.Default.ToReff(), "", "", DateTime.MinValue);

    public static TrsBill2FinalizationEventType Create(
        int noUrut,
        TrsBill2KomponenType komponen,
        TrsBillJenisBayarType jenisBayar,
        decimal nilai,
        PpaReff petugasMedis,
        string petugasKasir,
        string trsBayarId,
        DateTime tglBayar)
    {
        if (komponen == TrsBill2KomponenType.Default)
            throw new ArgumentException("Komponen must not be default", nameof(komponen));

        if (jenisBayar != TrsBillJenisBayarType.Kas &&
            jenisBayar != TrsBillJenisBayarType.Hut &&
            !jenisBayar.IsTipeJaminan)
            throw new ArgumentException("JenisBayar must be KAS, HUT, or a jenisBayar with IsTipeJaminan", nameof(jenisBayar));

        if (petugasKasir.Trim() == string.Empty)
            throw new ArgumentException("Petugas Kasir should not empty", nameof(jenisBayar));

        return new TrsBill2FinalizationEventType(noUrut, komponen, jenisBayar, nilai, petugasMedis, petugasKasir, trsBayarId, tglBayar);
    }

    public int NoUrut { get; init; }
    public TrsBill2KomponenType Komponen { get; init; }
    public TrsBillJenisBayarType JenisBayar { get; init; }
    public decimal Nilai { get; init; }
    public PpaReff PetugasMedis { get; init; }
    public string PetugasKasir { get; init; }
    public string TrsBayarId { get; init; }
    public DateTime TglBayar { get; init; }
}
