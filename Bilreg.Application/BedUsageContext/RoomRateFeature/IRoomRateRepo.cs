using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.RoomRateFeature;

public interface IRoomRateRepo<T> :
    ISaveChange<IRoomRate<T>>,
    ILoadEntity<IRoomRate<T>, IKamarKey>,
    IDeleteEntity<IKamarKey>,
    IListData<IRoomRate<T>>
        where T : IRoomRateDetail
{
    
}