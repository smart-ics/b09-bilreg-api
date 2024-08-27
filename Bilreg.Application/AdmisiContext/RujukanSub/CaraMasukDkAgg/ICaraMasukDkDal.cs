using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg
{
    public interface ICaraMasukDkDal :
        IGetData<CaraMasukDkModel, ICaraMasukDkKey>,
        IListData<CaraMasukDkModel>
    {

    }
}
