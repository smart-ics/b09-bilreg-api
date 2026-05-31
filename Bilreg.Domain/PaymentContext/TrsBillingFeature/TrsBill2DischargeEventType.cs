using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBill2DischargeEventType
{
    public TrsBill2DischargeEventType(int noUrut,
        TrsBill2KomponenType komponen,
        TrsBill2JenisBayarType jenisBayar,
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
    public static TrsBill2DischargeEventType Default 
        => new(0, TrsBill2KomponenType.Default, TrsBill2JenisBayarType.Default, 0, 
            PpaType.Default.ToReff(), "", "", DateTime.MinValue);

    public static TrsBill2DischargeEventType Create(
        int noUrut,
        TrsBill2KomponenType komponen,
        TrsBill2JenisBayarType jenisBayar,
        decimal nilai,
        PpaReff petugasMedis,
        string petugasKasir,
        string trsBayarId,
        DateTime tglBayar
    )
    {
        if (komponen == TrsBill2KomponenType.Default)
            throw new ArgumentException("Komponen must not be default", nameof(komponen));

        if (jenisBayar != TrsBill2JenisBayarType.Kas &&
            jenisBayar != TrsBill2JenisBayarType.Hut &&
            !jenisBayar.IsTipeJaminan)
            throw new ArgumentException("JenisBayar must be KAS, HUT, or a jenisBayar with IsTipeJaminan", nameof(jenisBayar));
        
        if (petugasKasir.Trim() == string.Empty)
            throw new ArgumentException("Petugas Kasir should not empty", nameof(jenisBayar));
        
        return new TrsBill2DischargeEventType(noUrut, komponen, jenisBayar, nilai, petugasMedis, petugasKasir, trsBayarId, tglBayar);
    }

    public int NoUrut { get; init; }
    public TrsBill2KomponenType Komponen { get; init; }
    public TrsBill2JenisBayarType JenisBayar { get; init; }
    public decimal Nilai { get; init; }
    public PpaReff PetugasMedis { get; init; }
    public string PetugasKasir { get; init; }
    public string TrsBayarId { get; init; }
    public DateTime TglBayar { get; init; }
}