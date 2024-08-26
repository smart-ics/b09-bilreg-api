using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public interface ICaraBayarDkDal: IGetData<CaraBayarDkModel, ICaraBayarDkKey>, IListData<CaraBayarDkModel>
{
}