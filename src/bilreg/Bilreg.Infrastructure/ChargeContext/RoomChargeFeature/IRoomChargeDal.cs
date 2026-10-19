using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public interface IRoomChargeDal :
    IInsert<RoomChargeDto>,
    IUpdate<RoomChargeDto>,
    IGetData<RoomChargeDto, IRoomChargeKey>
{
    RoomChargeDto? GetData(string roomChargeId);
    RoomChargeDto? GetByPakaiBedAndDate(string pakaiBedId, string chargeCycleDate);
    IEnumerable<RoomChargeDto> ListDataByReg(string regId);
    IEnumerable<RoomChargeDto> ListActiveByBangsalAndDate(string bangsalId, string chargeCycleDate);
}
