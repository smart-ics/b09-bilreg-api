using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature;

public interface IRoomChargeRepo :
    ISaveChange<RoomChargeModel>,
    ILoadEntity<RoomChargeModel, IRoomChargeKey>
{
    void Save(RoomChargeModel model);
    MayBe<RoomChargeModel> LoadEntity(string roomChargeId);
    MayBe<RoomChargeModel> GetByPakaiBedAndDate(string pakaiBedId, string chargeCycleDate);
    IEnumerable<RoomChargeModel> ListDataByReg(string regId);
    IEnumerable<RoomChargeModel> ListActiveByBangsalAndDate(string bangsalId, string chargeCycleDate);
}
