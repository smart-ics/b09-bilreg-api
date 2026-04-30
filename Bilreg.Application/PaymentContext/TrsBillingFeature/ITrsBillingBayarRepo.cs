using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public interface ITrsBillingBayarRepo :
    ISaveChange<IEnumerable<TrsBilling2Model>>,
    IDeleteEntity<ITrsBillingBayarKey>,
    IListData<TrsBilling2Model, IRegKey>
{
}
