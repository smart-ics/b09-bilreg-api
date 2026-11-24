using Ardalis.GuardClauses;

namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Model konfigurasi dan komputasi pembulatan nominal total invoice kasir.
/// Parameter persisten dibaca dari tabel legacy <c>tz_parameter_sistem</c>
/// (<c>FAR_DBUMUM_PEMBULATAN</c> dan <c>FAR_DBUMUM_PEMBULATAN_ATAS</c>).
/// </summary>
public record RoundingPolicyModel
{
    #region CREATION
    public RoundingPolicyModel(int nilaiPembulatan, RoundingModeEnum jenisPembulatan)
    {
        Guard.Against.NegativeOrZero(nilaiPembulatan, nameof(nilaiPembulatan));
        NilaiPembulatan = nilaiPembulatan;
        JenisPembulatan = jenisPembulatan;
    }

    /// <summary>
    /// Konfigurasi fallback standar: kelipatan Rp 100 dengan mode Ke Atas (Ceiling).
    /// </summary>
    public static RoundingPolicyModel Default => new(100, RoundingModeEnum.KeAtas);
    #endregion

    #region PROPERTIES
    /// <summary>Kelipatan target pembulatan total invoice (mis. 100, 500, 1000).</summary>
    public int NilaiPembulatan { get; init; }

    /// <summary>Arah pembulatan (Normal / Ke Atas).</summary>
    public RoundingModeEnum JenisPembulatan { get; init; }
    #endregion

    #region BEHAVIOUR
    /// <summary>
    /// Mengevaluasi total akhir tagihan dan selisih pembulatan secara presisi.
    /// </summary>
    /// <param name="totalBefore">Total tagihan sebelum pembulatan.</param>
    public PembulatanResultType HitungPembulatan(decimal totalBefore)
    {
        var increment = NilaiPembulatan;
        var kelipatanBawah = Math.Floor(totalBefore / increment) * increment;
        var pecahan = totalBefore - kelipatanBawah;
        var setengah = increment / 2m;

        var totalAkhir = JenisPembulatan switch
        {
            RoundingModeEnum.KeAtas => pecahan == 0m ? totalBefore : kelipatanBawah + increment,
            RoundingModeEnum.Normal => pecahan >= setengah ? kelipatanBawah + increment : kelipatanBawah,
            _ => throw new InvalidOperationException($"Jenis pembulatan '{JenisPembulatan}' tidak dikenal.")
        };

        return new PembulatanResultType(totalBefore, totalAkhir, totalAkhir - totalBefore);
    }
    #endregion
}

/// <summary>
/// Hasil evaluasi pembulatan total tagihan kasir.
/// </summary>
/// <param name="TotalSebelum">Total tagihan sebelum pembulatan.</param>
/// <param name="TotalAkhir">Total tagihan setelah pembulatan.</param>
/// <param name="SelisihPembulatan">Selisih yang dibukukan ke akun penyesuaian pembulatan kasir (<c>TotalAkhir - TotalSebelum</c>).</param>
public record PembulatanResultType(decimal TotalSebelum, decimal TotalAkhir, decimal SelisihPembulatan);
