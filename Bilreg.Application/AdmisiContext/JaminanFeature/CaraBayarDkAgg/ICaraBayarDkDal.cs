using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.CaraBayarDkAgg;

public interface ICaraBayarDkDal :
    IGetData<CaraBayarDkType, ICaraBayarDkKey>,
    IListData<CaraBayarDkType>
{
}