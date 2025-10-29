using Bilreg.Domain.AdmisiContext.JaminanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public interface ICaraBayarDkDal :
    IGetData<CaraBayarDkType, ICaraBayarDkKey>,
    IListData<CaraBayarDkType>
{
}