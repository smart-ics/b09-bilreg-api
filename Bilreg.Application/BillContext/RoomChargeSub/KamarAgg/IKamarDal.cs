using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;

public interface IKamarDal:
    IInsert<KamarModel>,
    IUpdate<KamarModel>,
    IDelete<IKamarKey>,
    IGetData<KamarModel,IKamarKey>,
    IListData<KamarModel,IBangsalKey>,
    IListData<KamarModel,IKelasKey>
{
    
}