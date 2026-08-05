using Farinv.Domain.BrgContext.BrgFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.BrgFeature;

public interface ISatuanRepo :
    ISaveChange<SatuanType>,
    ILoadEntity<SatuanType, ISatuanKey>,
    IDeleteEntity<ISatuanKey>,
    IListData<SatuanType>
{
}