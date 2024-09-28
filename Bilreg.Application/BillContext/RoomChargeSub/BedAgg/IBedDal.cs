using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.RoomChargeSub.BedAgg;

public interface IBedDal : 
    IInsert<BedModel>,
    IUpdate<BedModel>,
    IDelete<IBedKey>,
    IGetData<BedModel, IBedKey>,
    IListData<BedModel, IBangsalKey>
{
}