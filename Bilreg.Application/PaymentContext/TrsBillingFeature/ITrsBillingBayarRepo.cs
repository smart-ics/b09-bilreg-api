using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public interface ITrsBillingBayarRepo :
    ISaveChange<IEnumerable<TrsBilling2Base>>,
    IListData<TrsBilling2Base, IRegKey>
{
    void DeleteByPaymentId(string paymentId);
}