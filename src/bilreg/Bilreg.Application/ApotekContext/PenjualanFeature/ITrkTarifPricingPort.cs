namespace Bilreg.Application.ApotekContext.PenjualanFeature;

/// <summary>
/// Parameter permintaan evaluasi penetapan harga satuan barang persediaan farmasi.
/// </summary>
public record TrkTarifItemPricingRequest(
    string BrgId,
    string TipeJaminanId,
    DateTime TransactionDate,
    decimal Qty = 1,
    string SatuanId = "");

/// <summary>
/// Hasil evaluasi kalkulasi harga komersial barang, tuslah /r, embalase /R, diskon, dan snapshot HNA/HET.
/// </summary>
public record TrkTarifItemPricingResult(
    string BrgId,
    string TipeHargaId,
    decimal HargaSatuan,
    decimal DiskonPersen,
    decimal BiayaPerObat_r,
    decimal BiayaPerResep_R,
    decimal HnaApplied,
    decimal MarginPersenApplied,
    bool IsFixedPriceApplied,
    bool IsHetCapped,
    decimal HetNominal);

/// <summary>
/// Hasil evaluasi pembulatan nominal total tagihan penjualan kasir apotek.
/// </summary>
public record TrkTarifRoundingResult(
    decimal TotalBeforeRounding,
    decimal TotalAfterRounding,
    decimal RoundingDifference);

/// <summary>
/// Port abstraksi kalkulasi penetapan harga jual obat/BMHP dan pembulatan kas tagihan
/// sesuai kebijakan sentral OC-TRK-PRICING-POLICY-BARANG.
/// </summary>
public interface ITrkTarifPricingPort
{
    /// <summary>
    /// Menghitung harga jual satuan final, diskon default, dan biaya jasa /r /R secara asinkron.
    /// </summary>
    Task<TrkTarifItemPricingResult> CalculateItemPriceAsync(
        TrkTarifItemPricingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Menghitung harga jual satuan final, diskon default, dan biaya jasa /r /R secara sinkron.
    /// </summary>
    TrkTarifItemPricingResult CalculateItemPrice(TrkTarifItemPricingRequest request);

    /// <summary>
    /// Mengevaluasi pembulatan nominal total tagihan kasir berdasarkan kelipatan dan mode pembulatan.
    /// </summary>
    TrkTarifRoundingResult CalculateRounding(
        decimal totalBeforeRounding,
        int roundingIncrement = 100,
        int roundingMode = 1);
}

/// <summary>
/// Implementasi fail-closed default bila port pricing belum dikonfigurasi.
/// </summary>
public class FailClosedTrkTarifPricingPort : ITrkTarifPricingPort
{
    public Task<TrkTarifItemPricingResult> CalculateItemPriceAsync(
        TrkTarifItemPricingRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromException<TrkTarifItemPricingResult>(
            new InvalidOperationException($"ITrkTarifPricingPort adapter is not configured. Unable to calculate price for item '{request.BrgId}'."));

    public TrkTarifItemPricingResult CalculateItemPrice(TrkTarifItemPricingRequest request)
        => throw new InvalidOperationException($"ITrkTarifPricingPort adapter is not configured. Unable to calculate price for item '{request.BrgId}'.");

    public TrkTarifRoundingResult CalculateRounding(
        decimal totalBeforeRounding,
        int roundingIncrement = 100,
        int roundingMode = 1)
    {
        if (roundingIncrement <= 0)
            return new TrkTarifRoundingResult(totalBeforeRounding, totalBeforeRounding, 0);

        decimal rounded;
        if (roundingMode == 2) // Rounding Up / Ke Atas
        {
            rounded = Math.Ceiling(totalBeforeRounding / roundingIncrement) * roundingIncrement;
        }
        else // Rounding Normal / Away From Zero
        {
            rounded = Math.Round(totalBeforeRounding / roundingIncrement, MidpointRounding.AwayFromZero) * roundingIncrement;
        }
        return new TrkTarifRoundingResult(totalBeforeRounding, rounded, rounded - totalBeforeRounding);
    }
}
