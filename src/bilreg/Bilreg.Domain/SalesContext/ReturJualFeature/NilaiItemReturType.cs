namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public record NilaiItemReturType
{
    public NilaiItemReturType(
        decimal hargaJual,
        decimal hargaRetur,
        decimal taxPerUnit,
        decimal subTotalJual,
        decimal subTotalRetur,
        decimal subTotalTax,
        decimal total)
    {
        HargaJual = hargaJual;
        HargaRetur = hargaRetur;
        TaxPerUnit = taxPerUnit;
        SubTotalJual = subTotalJual;
        SubTotalRetur = subTotalRetur;
        SubTotalTax = subTotalTax;
        Total = total;
    }
    public decimal HargaJual { get; init; }
    public decimal HargaRetur { get; init; }
    public decimal TaxPerUnit { get; init; }
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