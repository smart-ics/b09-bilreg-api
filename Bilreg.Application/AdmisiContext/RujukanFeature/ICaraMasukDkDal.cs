using Bilreg.Domain.AdmisiContext.RujukanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;

public interface ICaraMasukDkRepo :
    ILoadEntity<CaraMasukDkType, ICaraMasukDkKey>,
    IListData<CaraMasukDkType>
{
}
