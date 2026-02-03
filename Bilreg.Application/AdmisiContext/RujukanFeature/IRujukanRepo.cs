using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;

public interface IRujukanRepo :
    ILoadEntity<RujukanType, IRujukanKey>,
    ILoadEntity<RujukanType, IPpkKey>,
    IListData<RujukanType, ITipeRujukanKey>,
    IListData<RujukanType, ICaraMasukDkKey>
{
}
