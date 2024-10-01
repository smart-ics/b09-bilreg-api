using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;

public interface ITipeKamarDal :
    IInsert<TipeKamarModel>,
    IUpdate<TipeKamarModel>,
    IDelete<ITipeKamarKey>,
    IGetData<TipeKamarModel,ITipeKamarKey>,
    IListData<TipeKamarModel>
{
    
}