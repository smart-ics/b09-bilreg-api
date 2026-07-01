using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PpaFeature;

public interface IGroupSpesialisRepo :
    ILoadEntity<GroupSpesialisType, IGroupSpesialisKey>,
    IListData<GroupSpesialisType>
{
}
