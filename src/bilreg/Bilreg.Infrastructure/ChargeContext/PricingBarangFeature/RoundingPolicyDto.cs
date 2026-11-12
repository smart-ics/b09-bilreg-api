using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

/// <summary>
/// DTO konfigurasi pembulatan kasir yang dipetakan dari dua baris parameter legacy pada tabel
/// <c>tz_parameter_sistem</c> (<c>FAR_DBUMUM_PEMBULATAN</c> dan <c>FAR_DBUMUM_PEMBULATAN_ATAS</c>).
/// </summary>
public record RoundingPolicyDto(int NilaiPembulatan, RoundingModeEnum JenisPembulatan)
{
    public static RoundingPolicyDto FromModel(RoundingPolicyModel model) => new(
        model.NilaiPembulatan,
        model.JenisPembulatan);

    public RoundingPolicyModel ToModel() => new(
        NilaiPembulatan,
        JenisPembulatan);
}
