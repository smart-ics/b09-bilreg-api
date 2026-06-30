using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public interface IPasienBalanceLoader
{
    PasienBalanceModel Load(IPasienKey key);
}
