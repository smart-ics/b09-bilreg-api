using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public interface ITrsBillingRepo :
    ISaveChange<TrsBillType>,
    ILoadEntity<TrsBillType, ITrsBillingKey>,
    IDeleteEntity<ITrsBillingKey>,
    IListData<TrsBillType, IRegKey>
{
}
