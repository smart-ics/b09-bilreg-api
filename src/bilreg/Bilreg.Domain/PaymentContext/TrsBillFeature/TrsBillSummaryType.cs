using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Domain.PaymentContext.TrsBillFeature;

public record TrsBillSummaryType
{
    public TrsBillSummaryType(int noUrut, BillSumType billSum, decimal nilaiJasa, decimal nilaiObat)
    {
        NoUrut = noUrut;
        BillSum = billSum;
        NilaiJasa = nilaiJasa;
        NilaiObat = nilaiObat;
    }

    public int NoUrut { get; init; }
    public BillSumType BillSum { get; init; }
    public decimal NilaiJasa { get; init; }
    public decimal NilaiObat { get; init; }
}


public record BillSumType(string BillSumCode, string BillSumDescription)
{
    public static BillSumType ToBill => new("ToBill", "Bill Dalam Perawatan");
    public static BillSumType ToDisk => new("ToDisk", "Total Diskon");
    public static BillSumType ToTax => new("ToTax", "Total Tax");
    public static BillSumType ToBia => new("ToBia", "Total Biaya Plus");

}