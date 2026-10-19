using Ardalis.GuardClauses;

namespace Bilreg.Domain.ChargeContext.RoomChargeFeature;

public record RoomChargeKomponenModel(
    string KomponenId,
    string KomponenName,
    string KomponenCategory,
    decimal Tarif,
    decimal Diskon,
    decimal Total,
    string RekeningPendapatan)
{
    public static RoomChargeKomponenModel Create(
        string komponenId,
        string komponenName,
        string komponenCategory,
        decimal tarif,
        decimal diskon = 0,
        string rekeningPendapatan = "")
    {
        Guard.Against.NullOrWhiteSpace(komponenId, nameof(komponenId));
        Guard.Against.NullOrWhiteSpace(komponenName, nameof(komponenName));

        var category = string.IsNullOrWhiteSpace(komponenCategory) ? "Akomodasi" : komponenCategory;
        var total = tarif - diskon;

        return new RoomChargeKomponenModel(
            komponenId,
            komponenName,
            category,
            tarif,
            diskon,
            total,
            rekeningPendapatan ?? string.Empty);
    }
}
