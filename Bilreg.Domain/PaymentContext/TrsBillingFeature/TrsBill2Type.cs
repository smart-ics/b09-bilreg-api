namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBill2TransEventType(
    int NoUrut,
    TrsBill2KompType Komponen);

public class TrsBill2DischargeEventType
{
    
}

public class TrsBill2PaymentEventType
{
    
}

public record TrsBill2KompType(
    string BillKompId,
    string BillKompName);

public record TrsBill2JenisBayarType(
    string JenisBayarId,
    string JenisBayarName)
{
    public static TrsBill2JenisBayarType Pdp => new("PDP", "Pendapatan");
    public static TrsBill2JenisBayarType Pot => new("POT", "Potongan");
    public static TrsBill2JenisBayarType Byl => new("BYL", "Biaya Lain");
    public static TrsBill2JenisBayarType Tax => new("TAX", "Tax");
    
    public static TrsBill2JenisBayarType Hut => new("HUT", "Hutang");
    public static TrsBill2JenisBayarType Kas => new("KAS", "Kas");
    
};