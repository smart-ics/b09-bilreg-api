using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Nuna.Lib.DataAccessHelper;
namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRek
{
    public interface ITipeRekDal :
    IGetData<TipeRekModel, ITipeRekKey>,
    IListData<TipeRekModel>
    {
    }

}
