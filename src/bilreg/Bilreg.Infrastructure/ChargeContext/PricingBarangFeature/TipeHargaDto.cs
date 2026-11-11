using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

/// <summary>
/// DTO master klasifikasi tipe harga penjamin pada tabel legacy <c>tb_tipe_barang</c>.
/// Memetakan <c>DefaultMarginPersen</c> ke kolom <c>fn_default_profit</c> sesuai arsitektur,
/// serta jasa kefarmasian Tuslah <c>/r</c> (<c>fn_biaya_per_barang</c>) dan
/// Embalase <c>/R</c> (<c>fn_biaya_per_racik</c>).
/// </summary>
public record TipeHargaDto(
    string fs_kd_tipe_barang,
    string fs_nm_tipe_barang,
    decimal fn_default_profit,
    decimal fn_biaya_per_barang,
    decimal fn_biaya_per_racik,
    decimal fn_tax,
    decimal fn_diskon,
    bool fb_aktif)
{
    public static TipeHargaDto FromModel(TipeHargaModel model) => new(
        model.TipeHargaId,
        model.TipeHargaName,
        model.DefaultMarginPersen,
        model.BiayaPerObat_r,
        model.BiayaPerResep_R,
        model.TaxPersen,
        model.DiskonPersen,
        model.IsActive);

    public TipeHargaModel ToModel() => new(
        fs_kd_tipe_barang,
        fs_nm_tipe_barang,
        fn_default_profit,
        fn_biaya_per_barang,
        fn_biaya_per_racik,
        fn_tax,
        fn_diskon,
        fb_aktif);
}
