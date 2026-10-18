using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature;

/// <summary>
/// Port pembacaan dan pembaruan konfigurasi pembulatan nominal total tagihan kasir pada tabel
/// legacy <c>tz_parameter_sistem</c> (parameter <c>FAR_DBUMUM_PEMBULATAN</c> dan
/// <c>FAR_DBUMUM_PEMBULATAN_ATAS</c>), dengan fallback default <c>100</c> / <c>Ke Atas</c>
/// (ARCHITECTURE TD-07).
/// </summary>
public interface IRoundingPolicyRepo
{
    /// <summary>
    /// Mengembalikan konfigurasi pembulatan aktif. Bila salah satu parameter tidak ditemukan,
    /// nilai fallback default (<c>NilaiPembulatan = 100</c>, <c>JenisPembulatan = KeAtas</c>)
    /// digunakan.
    /// </summary>
    RoundingPolicyModel GetPolicy();

    /// <summary>
    /// Menyimpan (upsert) konfigurasi pembulatan ke <c>tz_parameter_sistem</c>.
    /// </summary>
    void SavePolicy(RoundingPolicyModel model);
}
