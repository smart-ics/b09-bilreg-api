using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature;

/// <summary>
/// Port/kontrak domain service kalkulasi harga satuan barang dan evaluasi pembulatan total
/// invoice kasir (ARCHITECTURE TD-06). Dikonsumsi langsung oleh modul Apotek (<c>APT-BILL</c>)
/// dan Billing Tata Rekening (<c>TRK-BILLING</c>) tanpa menduplikasi logika tarif.
/// </summary>
public interface IItemPricingCalculator
{
    /// <summary>
    /// Menghitung harga jual satuan final suatu barang untuk tipe harga/penjamin tertentu pada
    /// tanggal transaksi tertentu, dengan menerapkan prioritas override Harga Jual Fix,
    /// pemotongan wajib plafon HET, dan konversi satuan UOM dua tingkat (ARCHITECTURE TD-03).
    /// </summary>
    /// <param name="barangId">Kode barang persediaan.</param>
    /// <param name="tipeHargaId">Kode tipe harga / tipe penjamin (mis. <c>UM</c>, <c>JM</c>, <c>KW</c>).</param>
    /// <param name="transactionDate">Tanggal transaksi untuk pembacaan snapshot HNA aktif.</param>
    /// <param name="qty">Kuantitas yang diminta (default 1).</param>
    /// <param name="satuanId">Satuan yang diminta; kosong berarti satuan dasar/terkecil.</param>
    /// <param name="cancellationToken">Token pembatalan.</param>
    /// <exception cref="HnaNotFoundException">
    /// Dilempar bila tidak ada snapshot HNA aktif (ASM-006) dan barang tidak memiliki override
    /// Harga Jual Fix (<c>&gt; 0</c>) sebagai harga sementara.
    /// </exception>
    Task<ItemPriceCalculationResultDto> CalculateAsync(
        string barangId,
        string tipeHargaId,
        DateTime transactionDate,
        int qty = 1,
        string satuanId = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mengevaluasi total akhir tagihan kasir dan selisih pembulatan secara presisi
    /// (ARCHITECTURE TD-07 / AC-ARCH-06).
    /// </summary>
    /// <param name="totalBeforeRounding">Total tagihan sebelum pembulatan.</param>
    /// <param name="roundingIncrement">Kelipatan target pembulatan (mis. 100, 500, 1000).</param>
    /// <param name="roundingMode">Arah pembulatan (Normal / Ke Atas).</param>
    RoundingEvaluationResultDto CalculateRounding(
        decimal totalBeforeRounding,
        int roundingIncrement,
        RoundingModeEnum roundingMode);
}

/// <summary>
/// Exception yang dilempar ketika snapshot HNA aktif tidak ditemukan untuk suatu barang dan
/// penetapan override Harga Jual Fix juga tidak tersedia (ASM-006 / outcome EX-PRC-01).
/// </summary>
public class HnaNotFoundException : Exception
{
    public HnaNotFoundException(string barangId)
        : base($"HNA aktif tidak ditemukan untuk barang '{barangId}'. Tetapkan Harga Jual Fix atau terbitkan batch HNA terlebih dahulu.")
    {
        BarangId = barangId;
    }

    /// <summary>Kode barang yang tidak memiliki snapshot HNA aktif.</summary>
    public string BarangId { get; }
}
