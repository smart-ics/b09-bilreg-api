namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBill2CoaType
{
    public TrsBill2CoaType(CoaType ppdp, CoaType pdpt, CoaType persediaan, CoaType pdptLain, CoaType tax, CoaType disc)
    {
        Ppdp = ppdp;
        Pdpt = pdpt;
        Persediaan = persediaan;
        PdptLain = pdptLain;
        Tax = tax;
        Disc = disc;
    }
    public static TrsBill2CoaType Default => new(CoaType.Default, CoaType.Default, CoaType.Default, 
        CoaType.Default, CoaType.Default, CoaType.Default);
    public CoaType Ppdp { get; init; }
    public CoaType Pdpt { get; init; }
    public CoaType Persediaan { get; init; }
    public CoaType PdptLain { get; init; }
    public CoaType Tax { get; init; }
    public CoaType Disc { get; init; }
}