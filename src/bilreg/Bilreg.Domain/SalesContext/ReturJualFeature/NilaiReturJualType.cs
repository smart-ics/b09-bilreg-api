namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public record NilaiReturJualType
{
    public NilaiReturJualType(
        decimal sumSubTotalJual,
        decimal sumSubTotalRetur,
        decimal sumTax,
        decimal pembulatan,
        decimal grandTotal)
    {
        SumSubTotalJual = sumSubTotalJual;
        SumSubTotalRetur = sumSubTotalRetur;
        SumTax = sumTax;
        Pembulatan = pembulatan;
        GrandTotal = grandTotal;
    }
    public decimal SumSubTotalJual { get; init; }
    public decimal SumSubTotalRetur { get; init; }
    public decimal SumTax { get; init; }
    public decimal Pembulatan { get; init; }
    public decimal GrandTotal { get; init; }

    public static NilaiReturJualType Default => new(0, 0, 0, 0, 0);

    public static NilaiReturJualType RecalcFrom(
        IEnumerable<ReturJualItemType> items,
        decimal pembulatan = 0)
    {
        var active = items.Where(x => !x.IsVoided).ToList();
        var sumSubTotalJual = active.Sum(x => x.Nilai.SubTotalJual);
        var sumSubTotalRetur = active.Sum(x => x.Nilai.SubTotalRetur);
        var sumTax = active.Sum(x => x.Nilai.SubTotalTax);
        var grandTotal = sumSubTotalRetur + sumTax + pembulatan;
        return new(sumSubTotalJual, sumSubTotalRetur, sumTax, pembulatan, grandTotal);
    }
}
