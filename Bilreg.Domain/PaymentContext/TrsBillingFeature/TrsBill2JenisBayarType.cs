namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBill2JenisBayarType(
    string JenisBayarId,
    string JenisBayarName,
    bool IsTipeJaminan)
{
    public static TrsBill2JenisBayarType Default => new("", "", false);
    public static TrsBill2JenisBayarType Pdp => new("PDP", "Pendapatan", false);
    public static TrsBill2JenisBayarType Pot => new("POT", "Potongan", false);
    public static TrsBill2JenisBayarType Byl => new("BYL", "Biaya Lain", false);
    public static TrsBill2JenisBayarType Tax => new("TAX", "Tax", false);
    
    public static TrsBill2JenisBayarType Hut => new("HUT", "Hutang", false);
    public static TrsBill2JenisBayarType Kas => new("KAS", "Kas", false);
}