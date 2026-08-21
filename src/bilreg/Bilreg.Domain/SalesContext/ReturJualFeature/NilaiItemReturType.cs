namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public class NilaiItemReturType
{
    public NilaiItemReturType(
        decimal hargaJual,
        decimal hargaRetur,
        decimal tax,
        decimal subTotalJual,
        decimal subTotalRetur,
        decimal subTotalTax,
        decimal total)
    {
        HargaRetur = hargaJual;
        HargaJual = hargaRetur;
        Tax = tax;
        SubTotalJual = subTotalJual;
        SubTotalRetur = subTotalRetur;
        SubTotalTax = subTotalTax;
        Total = total;
    }
    public decimal HargaJual { get; init; }
    public decimal HargaRetur { get; init; }
    public decimal Tax { get; init; }
    public decimal SubTotalRetur { get; init; }
    public decimal SubTotalJual { get; init; }
    public decimal SubTotalTax { get; init; }
    public decimal Total { get; init; }

    public static NilaiItemReturType Default => new(0, 0, 0, 0, 0, 0, 0);

    public static NilaiItemReturType Create(
        decimal qty,
        decimal hargaJual,
        decimal hargaRetur,
        decimal tax)
    {
        var subTotaJual = qty * hargaJual;
        var subTotalRetur =  qty * hargaRetur;
        var subTotalTax = qty * tax;
        var total = subTotalRetur + subTotalTax;
        return new(hargaJual, hargaRetur, tax, subTotaJual, subTotalRetur, subTotalTax, total);
    }
}