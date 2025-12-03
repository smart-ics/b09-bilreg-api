using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.WardFeature;

public interface IRoomCatRepo :
    ISaveChange<RoomCatType>,
    ILoadEntity<RoomCatType, IRoomCatKey>,
    IDeleteEntity<IRoomCatKey>,
    IListData<RoomCatType>
{
}