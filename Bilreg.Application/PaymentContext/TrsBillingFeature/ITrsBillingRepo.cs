using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public interface ITrsBillingRepo :
    ISaveChange<TrsBillingType>,
    ILoadEntity<TrsBillingType, ITrsBillingKey>,
    IDeleteEntity<ITrsBillingKey>,
    IListData<TrsBillingView, IRegKey>

{
}