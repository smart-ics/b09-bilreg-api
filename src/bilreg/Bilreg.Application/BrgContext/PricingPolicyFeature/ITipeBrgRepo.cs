using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.PricingPolicyFeature;

public interface ITipeBrgRepo :
    ISaveChange<TipeBrgType>,
    ILoadEntity<TipeBrgType, ITipeBrgKey>,
    IDeleteEntity<ITipeBrgKey>,
    IListData<TipeBrgType>
{
}
