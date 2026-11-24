using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

/// <summary>
/// DTO baris matriks harga per tipe penjamin pada tabel legacy <c>tb_barang_tipe</c>.
/// Composite key: <c>fs_kd_barang</c> + <c>fs_kd_tipe_harga</c>.
/// </summary>
public record ItemPricingVariantDto(
    string fs_kd_barang,
    string fs_kd_tipe_harga,
    decimal fn_harga_jual,
    decimal fn_profit,
    decimal fn_biaya,
    decimal fn_tax,
    decimal fn_diskon,
    bool fb_jual)
{
    public static ItemPricingVariantDto FromModel(string barangId, ItemPricingVariantType model) => new(
        barangId,
        model.TipeHargaId,
        model.HargaJualFix,
        model.MarginPersen,
        model.BiayaTambahanItem,
        model.TaxPersen,
        model.DiskonDefaultPersen,
        model.IsActive);

    public ItemPricingVariantType ToModel() => new(
        fs_kd_tipe_harga,
        fn_harga_jual,
        fn_profit,
        fn_biaya,
        fn_tax,
        fn_diskon,
        fb_jual);
}
