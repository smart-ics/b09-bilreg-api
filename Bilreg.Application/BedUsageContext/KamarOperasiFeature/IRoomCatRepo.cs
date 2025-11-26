using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IRoomCatRepo :
    ISaveChange<RoomCatModel>,
    ILoadEntity<RoomCatModel, IRoomCatKey>,
    IDeleteEntity<IRoomCatKey>,
    IListData<RoomCatModel>
{
}