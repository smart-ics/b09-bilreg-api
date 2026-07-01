using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public record TataRekeningPaymentType
{
    public TataRekeningPaymentType(PaymentType payment, decimal nilaiJasa, decimal nilaiObat, CoaType coa)
    {
        Payment = payment;
        NilaiJasa = nilaiJasa;
        NilaiObat = nilaiObat;
        Coa = coa;
    }
    public PaymentType Payment { get; init; }
    public decimal NilaiJasa { get; init; }
    public decimal NilaiObat { get; init; }
    public CoaType Coa { get; init; }
}