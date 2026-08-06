using Bilreg.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.StandardFeature;

public interface IOriginalRepo :
    ISaveChange<OriginalType>,
    ILoadEntity<OriginalType, IOriginalKey>,
    IDeleteEntity<IOriginalKey>,
    IListData<OriginalType>
{
}
