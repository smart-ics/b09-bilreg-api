namespace Bilreg.Domain.SalesContext.PenjualanFeature;

public record NilaiPenjualanType
{
    public NilaiPenjualanType(
        decimal sumSubTotal,
        decimal sumBiaya,
        decimal sumTax,
        decimal subTotal,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan,
        decimal bulat,
        decimal grandTotal)
    {
        SumSubTotal = sumSubTotal;
        SumBiaya = sumBiaya;
        SumTax = sumTax;
        SubTotal = subTotal;
        DiskonLain = diskonLain;
        BiayaLain = biayaLain;
        Pembulatan = pembulatan;
        Bulat = bulat;
        GrandTotal = grandTotal;
    }

    public decimal SumSubTotal { get; init; }
    public decimal SumBiaya { get; init; }
    public decimal SumTax { get; init; }
    public decimal SubTotal { get; init; }
    public decimal DiskonLain { get; init; }
    public decimal BiayaLain { get; init; }
    public decimal Pembulatan { get; init; }
    public decimal Bulat { get; init; }
    public decimal GrandTotal { get; init; }

    public static NilaiPenjualanType Default => new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    public static NilaiPenjualanType RecalcFrom(
        IEnumerable<PenjualanItemType> items,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan = 0,
        decimal bulat = 0)
    {
        var active = items.Where(x => !x.IsVoided).ToList();
        var sumSubTotal = active.Sum(x => x.Nilai.SubTotal);
        var sumBiaya = active.Sum(x => x.Nilai.Embalase);
        var sumTax = active.Sum(x => x.Nilai.Tax);
        var subTotal = sumSubTotal + sumBiaya + sumTax;
        var grandTotal = subTotal - diskonLain + biayaLain + pembulatan + bulat;
        return new(sumSubTotal, sumBiaya, sumTax, subTotal, diskonLain, biayaLain, pembulatan, bulat, grandTotal);
    }
}
