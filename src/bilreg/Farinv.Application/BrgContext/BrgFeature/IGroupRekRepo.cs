using Farinv.Domain.BrgContext.BrgFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.BrgFeature;

public interface IGroupRekRepo :
    ISaveChange<GroupRekType>,
    ILoadEntity<GroupRekType, IGroupRekKey>,
    IDeleteEntity<IGroupRekKey>,
    IListData<GroupRekType>
{
}