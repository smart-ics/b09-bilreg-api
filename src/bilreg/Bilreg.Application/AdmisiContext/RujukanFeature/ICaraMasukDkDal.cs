using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RujukanFeature;

public interface ICaraMasukDkRepo :
    ILoadEntity<CaraMasukDkType, ICaraMasukDkKey>,
    IListData<CaraMasukDkType>
{
}
