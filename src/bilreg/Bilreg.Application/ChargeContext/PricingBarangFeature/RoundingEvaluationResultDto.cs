using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature;

/// <summary>
/// Hasil evaluasi pembulatan total tagihan kasir (ARCHITECTURE TD-07 / AC-ARCH-06).
/// </summary>
/// <param name="TotalBeforeRounding">Total tagihan sebelum pembulatan.</param>
/// <param name="TotalAfterRounding">Total tagihan final setelah pembulatan.</param>
/// <param name="RoundingDifference">Selisih pembulatan yang dibukukan ke akun penyesuaian kasir (<c>TotalAfterRounding - TotalBeforeRounding</c>).</param>
/// <param name="RoundingIncrement">Kelipatan pembulatan yang dipakai.</param>
/// <param name="RoundingMode">Arah pembulatan yang dipakai.</param>
public record RoundingEvaluationResultDto(
    decimal TotalBeforeRounding,
    decimal TotalAfterRounding,
    decimal RoundingDifference,
    int RoundingIncrement,
    RoundingModeEnum RoundingMode);
