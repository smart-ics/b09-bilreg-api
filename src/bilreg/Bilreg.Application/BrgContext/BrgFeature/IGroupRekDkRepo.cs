using Bilreg.Domain.BrgContext.BrgFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.BrgFeature;

public interface IGroupRekDkRepo :
    ISaveChange<GroupRekDkType>,
    ILoadEntity<GroupRekDkType, IGroupRekDkKey>,
    IDeleteEntity<IGroupRekDkKey>,
    IListData<GroupRekDkType>
{
}