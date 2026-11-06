using Bilreg.Domain.ChargeContext.RoomChargeFeature;

namespace Bilreg.Application.ChargeContext.RoomChargeFeature;

public record RoomChargeTarifResult(
    decimal BaseTariff,
    IReadOnlyList<RoomChargeKomponenModel> KomponenList
);

public interface IRoomChargeTarifService
{
    RoomChargeTarifResult GetTarif(string billingClassId, string tipeJaminanId);
}
