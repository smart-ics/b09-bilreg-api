using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.RoomRateFeature;

public interface IRoomRateRepo :
    ISaveChange<IRoomRate<IRoomRateDetail>>,
    ILoadEntity<IRoomRate<IRoomRateDetail>, IKamarKey>,
    IDeleteEntity<IKamarKey>
{
}
