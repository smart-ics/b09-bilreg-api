using Bilreg.Domain.ChargeContext.RoomChargeFeature;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public record RoomChargeKomponenDto(
    string RoomChargeId,
    string KomponenId,
    string KomponenName,
    string KomponenCategory,
    decimal Tarif,
    decimal Diskon,
    decimal Total,
    string RekeningPendapatan)
{
    public static RoomChargeKomponenDto FromModel(RoomChargeKomponenModel model, string roomChargeId)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new RoomChargeKomponenDto(
            roomChargeId,
            model.KomponenId,
            model.KomponenName,
            model.KomponenCategory,
            model.Tarif,
            model.Diskon,
            model.Total,
            model.RekeningPendapatan);
    }

    public RoomChargeKomponenModel ToModel()
    {
        return new RoomChargeKomponenModel(
            KomponenId,
            KomponenName,
            KomponenCategory,
            Tarif,
            Diskon,
            Total,
            RekeningPendapatan);
    }
}
