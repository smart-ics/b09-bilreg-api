using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public interface IRoomChargeKomponenDal :
    IInsert<RoomChargeKomponenDto>,
    IInsertBulk<RoomChargeKomponenDto>,
    IListData<RoomChargeKomponenDto, IRoomChargeKey>
{
    void DeleteByRoomChargeId(string roomChargeId);
    IEnumerable<RoomChargeKomponenDto> ListDataByRoomChargeId(string roomChargeId);
}
