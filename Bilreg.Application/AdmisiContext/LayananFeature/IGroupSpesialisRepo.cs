using Bilreg.Domain.AdmisiContext.LayananFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.LayananFeature;

public interface IGroupSpesialisRepo :
    ILoadEntity<GroupSpesialisType, IGroupSpesialisKey>,
    IListData<GroupSpesialisType>
{
}
