using Farinv.Domain.BrgContext.BrgFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.BrgFeature;

public interface IGroupRekDkRepo :
    ISaveChange<GroupRekDkType>,
    ILoadEntity<GroupRekDkType, IGroupRekDkKey>,
    IDeleteEntity<IGroupRekDkKey>,
    IListData<GroupRekDkType>
{
}