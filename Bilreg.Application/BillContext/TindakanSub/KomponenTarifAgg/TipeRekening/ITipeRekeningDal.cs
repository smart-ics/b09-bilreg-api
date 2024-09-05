using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRekening
{
    public interface ITipeRekeningDal:
        IGetData<TipeRekeningModel,ITipeRekeningKey>,
        IListData<TipeRekeningModel>
    {
    }
}
