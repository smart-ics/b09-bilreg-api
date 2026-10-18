namespace Bilreg.Application.ChargeContext.PricingBarangFeature;

/// <summary>
/// Hasil kalkulasi harga jual satuan barang yang terurai lengkap (ARCHITECTURE TD-05 / TD-06).
/// Memisahkan harga komoditas murni dari jasa kefarmasian Tuslah <c>/r</c>
/// (<c>BiayaPerObat_r</c>) dan Embalase <c>/R</c> (<c>BiayaPerResep_R</c>), serta mengekspos
/// jejak keputusan harga (override Harga Jual Fix, pemotongan HET, dan konversi UOM).
/// </summary>
/// <param name="BarangId">Kode barang persediaan.</param>
/// <param name="TipeHargaId">Kode tipe harga / tipe penjamin yang dievaluasi.</param>
/// <param name="TransactionDate">Tanggal transaksi yang dipakai untuk pembacaan HNA aktif.</param>
/// <param name="Qty">Kuantitas yang diminta konsumen.</param>
/// <param name="SatuanId">Satuan yang diminta (kosong berarti satuan dasar / terkecil).</param>
/// <param name="Konversi">Rasio konversi UOM dua tingkat yang diterapkan (1 untuk satuan dasar).</param>
/// <param name="HnaApplied">Nilai HNA aktif yang dipakai (<c>0</c> bila tidak ada snapshot HNA dan Harga Jual Fix aktif).</param>
/// <param name="MarginPersenApplied">Persentase margin tipe penjamin yang dipakai (legacy <c>fn_profit</c>).</param>
/// <param name="HargaJualFixApplied">Nilai override Harga Jual Fix (legacy <c>fn_harga_jual</c>).</param>
/// <param name="IsFixedPriceApplied">True bila override Harga Jual Fix aktif mengabaikan formula HNA x margin.</param>
/// <param name="IsHetCapped">True bila harga dipotong menjadi senilai HET karena melampaui plafon legal.</param>
/// <param name="HetNominal">Plafon HET yang berlaku (<c>0</c> berarti tanpa plafon).</param>
/// <param name="HargaJualSatuanDasar">Harga jual final per satuan dasar sebelum konversi UOM.</param>
/// <param name="HargaJualFinal">Harga jual satuan final yang dibebankan ke pasien (setelah HET cap dan konversi UOM).</param>
/// <param name="DiskonDefaultPersen">Diskon default barang kepada pasien (legacy <c>fn_diskon</c>).</param>
/// <param name="BiayaTambahanItem">Biaya penanganan khusus item (legacy <c>fn_biaya</c>).</param>
/// <param name="BiayaPerObat_r">Jasa Tuslah per-obat <c>/r</c> (legacy <c>tb_tipe_barang.fn_biaya_per_barang</c>).</param>
/// <param name="BiayaPerResep_R">Jasa Embalase per-resep <c>/R</c> (legacy <c>tb_tipe_barang.fn_biaya_per_racik</c>).</param>
public record ItemPriceCalculationResultDto(
    string BarangId,
    string TipeHargaId,
    DateTime TransactionDate,
    int Qty,
    string SatuanId,
    decimal Konversi,
    decimal HnaApplied,
    decimal MarginPersenApplied,
    decimal HargaJualFixApplied,
    bool IsFixedPriceApplied,
    bool IsHetCapped,
    decimal HetNominal,
    decimal HargaJualSatuanDasar,
    decimal HargaJualFinal,
    decimal DiskonDefaultPersen,
    decimal BiayaTambahanItem,
    decimal BiayaPerObat_r,
    decimal BiayaPerResep_R);
