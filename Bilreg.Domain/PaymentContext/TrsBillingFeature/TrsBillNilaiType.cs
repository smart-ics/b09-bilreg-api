namespace Bilreg.Domain.PaymentContext.TrsBillingFeature;

public record TrsBillNilaiType(decimal SubTotal, decimal Diskon, decimal Tax, decimal Biaya)
{
    public static TrsBillNilaiType Default => new(0, 0, 0, 0);
    public decimal Total => SubTotal - Diskon + Biaya + Tax;
}