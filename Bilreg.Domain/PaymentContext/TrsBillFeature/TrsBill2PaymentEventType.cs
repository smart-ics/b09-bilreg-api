using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

public record TrsBill2PaymentEventType : ITrsBill2Event
{
    public TrsBill2PaymentEventType(int noUrut, 
        TrsBill2KomponenType komponen, 
        TrsBillJenisBayarType jenisBayar, 
        PaymentType payment, 
        decimal nilai, 
        DateTime tglBayar, PpaReff petugasMedis)
    {
        NoUrut = noUrut;
        Komponen = komponen;
        JenisBayar = jenisBayar;
        Payment = payment;
        Nilai = nilai;
        TglBayar = tglBayar;
        PetugasMedis = petugasMedis;
    }

    public static TrsBill2PaymentEventType Create(
        int noUrut, TrsBill2KomponenType komponen,
        TrsBillJenisBayarType jenisBayar, PaymentType payment, string paymentId,
        decimal nilai, DateTime tglBayar, PpaReff petugasMedis)
    {
        if (komponen == TrsBill2KomponenType.Default)
            throw new ArgumentException("Komponen must not be default", nameof(komponen));
        
        if (jenisBayar != TrsBillJenisBayarType.Kas &&
            jenisBayar != TrsBillJenisBayarType.Hut &&
            !jenisBayar.IsTipeJaminan)
            throw new ArgumentException("JenisBayar must be KAS, HUT, or a jenisBayar with IsTipeJaminan", nameof(jenisBayar));
        
        if (nilai < 0)
            throw new ArgumentException("Nilai must be non-negative", nameof(nilai));
        
        return new TrsBill2PaymentEventType(noUrut, komponen, jenisBayar, payment, nilai, tglBayar, petugasMedis);
    }
    
    public int NoUrut { get; init; }
    public TrsBill2KomponenType Komponen { get; init; }
    public TrsBillJenisBayarType JenisBayar { get; init; }
    public PaymentType Payment { get; init; }
    public decimal Nilai { get; init; }
    public DateTime TglBayar { get; init; }
    public PpaReff PetugasMedis { get; init; }
}