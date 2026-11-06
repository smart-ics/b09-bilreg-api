namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public interface ITrsDirectDal
{
    void UpdateTimestamp(string trsId, DateTime tglTrs, decimal genKeu);
}
