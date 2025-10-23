using Bilreg.Domain.AdmisiContext.RujukanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg
{
    public interface ICaraMasukDkDal :
        IGetDataMayBe<CaraMasukDkType, ICaraMasukDkKey>,
        IListDataMayBe<CaraMasukDkType>
    {
    }
}
