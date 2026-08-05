using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.StandardFeature;

public interface IOriginalRepo :
    ISaveChange<OriginalType>,
    ILoadEntity<OriginalType, IOriginalKey>,
    IDeleteEntity<IOriginalKey>,
    IListData<OriginalType>
{
}
