using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Query pembacaan konfigurasi pembulatan aktif total tagihan kasir (<c>tz_parameter_sistem</c>,
/// ARCHITECTURE TD-07).
/// </summary>
public record RoundingPolicyGetQuery() : IRequest<RoundingPolicyGetResponse>;

/// <summary>
/// Konfigurasi pembulatan aktif. Nilai fallback default <c>100</c> / <c>Ke Atas</c> diterapkan
/// oleh <see cref="IRoundingPolicyRepo.GetPolicy"/>.
/// </summary>
/// <param name="NilaiPembulatan">Kelipatan target pembulatan total invoice.</param>
/// <param name="JenisPembulatan">Arah pembulatan (Normal / Ke Atas).</param>
public record RoundingPolicyGetResponse(
    int NilaiPembulatan,
    RoundingModeEnum JenisPembulatan);
