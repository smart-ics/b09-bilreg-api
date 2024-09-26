using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public interface IBangsalDal :
    IInsert<BangsalModel>,
    IUpdate<BangsalModel>,
    IDelete<IBangsalKey>,
    IGetData<BangsalModel, IBangsalKey>,
    IListData<BangsalModel>
{
}