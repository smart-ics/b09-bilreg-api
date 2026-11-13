namespace Bilreg.Application.ChargeContext.PricingBarangFeature;

/// <summary>
/// Baris riwayat penerimaan pengadaan (tabel legacy <c>tb_trs_do</c> dan <c>tb_trs_do2</c>) sebagai
/// basis komputasi HPP/HNA. Nilai harga beli per satuan kecil dasar telah dikonversi dari satuan beli.
/// </summary>
public record PurchaseReceiptHnaDto(
    string ReceiptId,
    int NoUrut,
    string BarangId,
    string SatuanId,
    decimal Konversi,
    decimal Harga,
    decimal Diskon,
    decimal Tax,
    decimal Qty,
    decimal SubTotal,
    decimal Total,
    DateTime TglTrs,
    decimal HargaBeliPerSatuanKecil);

/// <summary>
/// Port pembacaan data historis faktur DO penerimaan pengadaan (tabel legacy <c>tb_trs_do</c> dan
/// <c>tb_trs_do2</c>) untuk pembentukan varian HNA pengadaan.
/// </summary>
public interface IPurchaseReceiptHnaQuery
{
    /// <summary>
    /// Membaca seluruh baris pengadaan non-void milik suatu barang, opsional dibatasi rentang tanggal
    /// dokumen penerimaan. Nilai <see cref="PurchaseReceiptHnaDto.HargaBeliPerSatuanKecil"/> dihitung
    /// sebagai harga beli per satuan kecil dasar.
    /// </summary>
    IEnumerable<PurchaseReceiptHnaDto> GetProcurementHistory(string barangId, DateTime? start = null, DateTime? end = null);
}
