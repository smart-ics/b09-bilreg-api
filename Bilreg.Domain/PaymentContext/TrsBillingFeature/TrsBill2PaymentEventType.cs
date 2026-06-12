namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBill2PaymentEventType
{
    public TrsBill2PaymentEventType(int noUrut, TrsBill2KomponenType komponen, 
        TrsBillJenisBayarType jenisBayar, decimal nilaiP, decimal nilaiN)
    {
        NoUrut = noUrut;
        Komponen = komponen;
        JenisBayar = jenisBayar;
        NilaiP = nilaiP;
        NilaiN = nilaiN;
    }

    public static TrsBill2PaymentEventType Create(
        int noUrut, TrsBill2KomponenType komponen,
        TrsBillJenisBayarType jenisBayar, 
        decimal nilaiP, decimal nilaiN
    )
    {
        if (komponen == TrsBill2KomponenType.Default)
            throw new ArgumentException("Komponen must not be default", nameof(komponen));
        
        if (jenisBayar != TrsBillJenisBayarType.Kas &&
            jenisBayar != TrsBillJenisBayarType.Hut &&
            !jenisBayar.IsTipeJaminan)
            throw new ArgumentException("JenisBayar must be KAS, HUT, or a jenisBayar with IsTipeJaminan", nameof(jenisBayar));
        
        if (nilaiP < 0 || nilaiN < 0)
            throw new ArgumentException("NilaiP and NilaiN must be non-negative", nameof(nilaiP) + " or " + nameof(nilaiN));
        
        if (nilaiP > 0 && nilaiN > 0)
            throw new ArgumentException("Only NilaiP or NilaiN can be set");
        
        var result = new TrsBill2PaymentEventType(noUrut, komponen, jenisBayar, nilaiP, nilaiN);
        return result;
    }
    
    public int NoUrut { get; init; }
    public TrsBill2KomponenType Komponen { get; init; }
    public TrsBillJenisBayarType JenisBayar { get; init; }
    public decimal NilaiP { get; init; }
    public decimal NilaiN { get; init; }
}