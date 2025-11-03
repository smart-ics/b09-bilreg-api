using Bilreg.Domain.AdmisiContext.RujukanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;

public interface IRujukanRepo :
    ILoadEntity<RujukanType, IRujukanKey>,
    IListData<RujukanType, ITipeRujukanKey>
{
}
