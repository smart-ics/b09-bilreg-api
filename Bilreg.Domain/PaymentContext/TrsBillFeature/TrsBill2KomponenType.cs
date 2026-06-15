namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

public record TrsBill2KomponenType
{
    public TrsBill2KomponenType(string billKompId, string billKompName)
    {
        BillKompId = billKompId;
        BillKompName = billKompName;
    }
    public static TrsBill2KomponenType Default => new("", "");
    public string BillKompId { get; init; }
    public string BillKompName { get; init; }
}