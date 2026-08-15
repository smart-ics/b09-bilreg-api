using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.RoomChargeFeature;

public interface IRoomChargeRepo :
    ISaveChange<RoomChargeModel>,
    ILoadEntity<RoomChargeModel, IRoomChargeKey>,
    IDelete<IRoomChargeKey>,
    IListData<RoomChargeView, IRegKey>,
    IListData<RoomChargeView, IPakaiBed>
{
}
