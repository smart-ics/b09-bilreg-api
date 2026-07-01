using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public interface IPasienBalanceLegacyReader
{
    IEnumerable<LegacyOutstandingReceivable> ListOutstanding(IPasienKey key);
}
