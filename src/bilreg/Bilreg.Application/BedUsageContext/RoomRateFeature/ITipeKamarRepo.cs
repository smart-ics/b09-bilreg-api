using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.RoomRateFeature;

public interface ITipeKamarRepo :
    ISaveChange<TipeKamarType>,
    ILoadEntity<TipeKamarType, ITipeKamarKey>,
    IDeleteEntity<ITipeKamarKey>,
    IListData<TipeKamarType>
{
}