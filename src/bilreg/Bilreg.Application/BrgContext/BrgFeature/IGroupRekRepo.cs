using Bilreg.Domain.BrgContext.BrgFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.BrgFeature;

public interface IGroupRekRepo :
    ISaveChange<GroupRekType>,
    ILoadEntity<GroupRekType, IGroupRekKey>,
    IDeleteEntity<IGroupRekKey>,
    IListData<GroupRekType>
{
}