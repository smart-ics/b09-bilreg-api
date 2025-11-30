using Bilreg.Domain.BedUsageContext.BangsalFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.BangsalFeature;

public interface IRoomCatRepo :
    ISaveChange<RoomCatType>,
    ILoadEntity<RoomCatType, IRoomCatKey>,
    IDeleteEntity<IRoomCatKey>,
    IListData<RoomCatType>
{
}