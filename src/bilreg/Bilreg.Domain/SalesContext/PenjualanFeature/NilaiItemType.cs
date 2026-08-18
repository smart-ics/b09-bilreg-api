namespace Bilreg.Domain.SalesContext.PenjualanFeature;

public record NilaiItemType
{
    public NilaiItemType(
        decimal harga,
        decimal diskon,
        decimal embalase,
        decimal subTotal,
        decimal taxProsen,
        decimal tax,
        decimal fee,
        decimal bulat,
        decimal nilaiKlaim,
        decimal total)
    {
        Harga = harga;
        Diskon = diskon;
        Embalase = embalase;
        SubTotal = subTotal;
        TaxProsen = taxProsen;
        Tax = tax;
        Fee = fee;
        Bulat = bulat;
        NilaiKlaim = nilaiKlaim;
        Total = total;
    }

    public decimal Harga { get; init; }
    public decimal Diskon { get; init; }
    public decimal Embalase { get; init; }
    public decimal SubTotal { get; init; }
    public decimal TaxProsen { get; init; }
    public decimal Tax { get; init; }
    public decimal Fee { get; init; }
    public decimal Bulat { get; init; }
    public decimal NilaiKlaim { get; init; }
    public decimal Total { get; init; }

    public static NilaiItemType Default => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public static NilaiItemType Create(
        decimal qty,
        decimal harga,
        decimal diskon,
        decimal embalase,
        decimal taxProsen,
        decimal tax,
        decimal fee,
        decimal bulat,
        decimal nilaiKlaim)
    {
        var subTotal = (qty * harga) - diskon;
        var total = subTotal + embalase + tax + fee + bulat;
        return new(harga, diskon, embalase, subTotal, taxProsen, tax, fee, bulat, nilaiKlaim, total);
    }
}
