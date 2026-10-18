using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Command pemutakhiran konfigurasi pembulatan nominal total tagihan kasir pada parameter global
/// legacy <c>tz_parameter_sistem</c> (<c>FAR_DBUMUM_PEMBULATAN</c> dan <c>FAR_DBUMUM_PEMBULATAN_ATAS</c>).
/// </summary>
public record RoundingPolicyUpdateCommand(
    int NilaiPembulatan,
    RoundingModeEnum JenisPembulatan) : IRequest<RoundingPolicyUpdateResponse>;

/// <summary>
/// Hasil pemutakhiran konfigurasi pembulatan kasir.
/// </summary>
/// <param name="NilaiPembulatan">Kelipatan pembulatan yang tersimpan.</param>
/// <param name="JenisPembulatan">Arah pembulatan yang tersimpan.</param>
public record RoundingPolicyUpdateResponse(
    int NilaiPembulatan,
    RoundingModeEnum JenisPembulatan);
