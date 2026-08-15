using Bilreg.Domain.BedUsageContext.RoomChargeFeature;

namespace Bilreg.Infrastructure.BedUsageContext.RoomChargeFeature;

public record RoomChargeKomponenDto(
    string fs_kd_trs,
    string fs_kd_detil_tarif, 
    decimal fn_tarif, decimal fn_diskon, decimal fn_total,
    string fs_nm_detil_tarif)
{
    public static RoomChargeKomponenDto FromModel(RoomChargeKomponenModel model, string roomChargeId)
    {
        var result = new RoomChargeKomponenDto(roomChargeId, model.DetilTarifId,
            model.Tarif, model.Diskon, model.Total, model.DetilTarifName);
        return result;
    }

    public RoomChargeKomponenModel ToModel()
    {
        var result = new RoomChargeKomponenModel(
            fs_kd_detil_tarif, fs_nm_detil_tarif, 
            fn_tarif, fn_diskon, fn_total);
        return result;
    }
}
