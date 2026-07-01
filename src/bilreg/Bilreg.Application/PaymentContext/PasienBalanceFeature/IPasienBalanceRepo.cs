using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.PasienBalanceFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public interface IPasienBalanceRepo :
    ISaveChange<PasienBalanceModel>,
    ILoadEntity<PasienBalanceModel, IPasienKey>
{
}
