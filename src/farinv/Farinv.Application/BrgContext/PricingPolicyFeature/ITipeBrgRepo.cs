using Farinv.Domain.BrgContext.PricingPolicyFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.PricingPolicyFeature;

public interface ITipeBrgRepo :
    ISaveChange<TipeBrgType>,
    ILoadEntity<TipeBrgType, ITipeBrgKey>,
    IDeleteEntity<ITipeBrgKey>,
    IListData<TipeBrgType>
{
}
