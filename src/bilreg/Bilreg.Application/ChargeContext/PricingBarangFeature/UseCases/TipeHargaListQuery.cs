using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Query pembacaan daftar seluruh master tipe harga penjamin (tabel legacy <c>tb_tipe_barang</c>).
/// </summary>
public record TipeHargaListQuery() : IRequest<IReadOnlyList<TipeHargaListResponse>>;

/// <summary>
/// Daftar varian tipe harga penjamin.
/// </summary>
/// <param name="TipeHargaId">Kode tipe harga / tipe penjamin.</param>
/// <param name="TipeHargaName">Nama resmi tipe harga penjamin.</param>
/// <param name="DefaultMarginPersen">Default margin % saat inisialisasi varian baru.</param>
/// <param name="BiayaPerObat_r">Jasa Tuslah per-obat <c>/r</c>.</param>
/// <param name="BiayaPerResep_R">Jasa Embalase per-resep <c>/R</c>.</param>
/// <param name="TaxPersen">Persentase pajak penjualan master.</param>
/// <param name="DiskonPersen">Persentase diskon master.</param>
/// <param name="IsActive">Status keaktifan tipe penjamin.</param>
public record TipeHargaListResponse(
    string TipeHargaId,
    string TipeHargaName,
    decimal DefaultMarginPersen,
    decimal BiayaPerObat_r,
    decimal BiayaPerResep_R,
    decimal TaxPersen,
    decimal DiskonPersen,
    bool IsActive);
