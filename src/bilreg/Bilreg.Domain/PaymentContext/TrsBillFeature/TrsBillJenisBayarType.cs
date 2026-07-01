namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

public record TrsBillJenisBayarType(
    string JenisBayarId,
    string JenisBayarName,
    bool IsTipeJaminan)
{
    public static TrsBillJenisBayarType Default => new("", "", false);
    public static TrsBillJenisBayarType Pdp => new("PDP", "Pendapatan", false);
    public static TrsBillJenisBayarType Pot => new("POT", "Potongan", false);
    public static TrsBillJenisBayarType Byl => new("BYL", "Biaya Lain", false);
    public static TrsBillJenisBayarType Tax => new("TAX", "Tax", false);
    
    public static TrsBillJenisBayarType Hut => new("HUT", "Hutang", false);
    public static TrsBillJenisBayarType Kas => new("KAS", "Kas", false);

    public static TrsBillJenisBayarType GetData(string jenisBayarId) => jenisBayarId switch
    {
        "PDP" => Pdp,
        "POT" => Pot,
        "BYL" => Byl,
        "TAX" => Tax,
        "HUT" => Hut,
        "KAS" => Kas,
        _ => new TrsBillJenisBayarType(jenisBayarId, jenisBayarId, true)
    };
}