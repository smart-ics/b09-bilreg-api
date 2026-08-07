using Bilreg.Domain.BrgContext.BrgFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.BrgFeature;

public interface ISatuanRepo :
    ISaveChange<SatuanType>,
    ILoadEntity<SatuanType, ISatuanKey>,
    IDeleteEntity<ISatuanKey>,
    IListData<SatuanType>
{
}