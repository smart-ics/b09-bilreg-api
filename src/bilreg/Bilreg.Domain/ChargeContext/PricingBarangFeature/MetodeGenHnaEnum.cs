namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Metode agregasi HNA yang mengonsolidasikan kumpulan HPP historis pengadaan menjadi satu nilai acuan.
/// Dipetakan ke kolom legacy <c>tb_barang.fs_metode_gen_hna</c>.
/// </summary>
public enum MetodeGenHnaEnum
{
    /// <summary>
    /// TERAKHIR (Last Purchase Price): nilai pembelian dari dokumen pengadaan paling mutakhir.
    /// Kode legacy <c>'1'</c>.
    /// </summary>
    Terakhir = 1,

    /// <summary>
    /// RATA-RATA (Average Purchase Price): rata-rata aritmatika seluruh transaksi pembelian.
    /// Kode legacy <c>'2'</c>.
    /// </summary>
    RataRata = 2,

    /// <summary>
    /// TERBESAR (Maximum Purchase Price): nilai pembelian tertinggi pada jendela evaluasi aktif.
    /// Kode legacy <c>'3'</c>.
    /// </summary>
    Terbesar = 3
}
