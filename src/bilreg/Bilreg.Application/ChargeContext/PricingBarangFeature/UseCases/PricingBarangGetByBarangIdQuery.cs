using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Query pembacaan matriks harga jual suatu barang di seluruh tipe harga/penjamin.
/// </summary>
/// <param name="BarangId">Kode barang persediaan.</param>
public record PricingBarangGetByBarangIdQuery(string BarangId)
    : IRequest<PricingBarangGetByBarangIdResponse>;

/// <summary>
/// Matriks harga jual barang pada seluruh tipe penjamin.
/// </summary>
/// <param name="BarangId">Kode barang persediaan.</param>
/// <param name="HetNominal">Plafon legal HET (<c>0</c> berarti tanpa plafon).</param>
/// <param name="MetodeGenHna">Metode agregasi HNA default barang.</param>
/// <param name="Variants">Daftar varian harga per tipe harga/penjamin.</param>
public record PricingBarangGetByBarangIdResponse(
    string BarangId,
    decimal HetNominal,
    MetodeGenHnaEnum MetodeGenHna,
    IReadOnlyList<PricingBarangVariantResponse> Variants);

/// <summary>
/// Varian harga satu tipe penjamin.
/// </summary>
/// <param name="TipeHargaId">Kode tipe harga / tipe penjamin.</param>
/// <param name="HargaJualFix">Override Harga Jual Fix (<c>0</c> berarti tidak aktif).</param>
/// <param name="MarginPersen">Persentase margin keuntungan tipe penjamin.</param>
/// <param name="BiayaTambahanItem">Biaya penanganan khusus item.</param>
/// <param name="TaxPersen">Persentase pajak penjualan.</param>
/// <param name="DiskonDefaultPersen">Diskon default item kepada pasien.</param>
/// <param name="IsActive">Status keaktifan varian tarif.</param>
public record PricingBarangVariantResponse(
    string TipeHargaId,
    decimal HargaJualFix,
    decimal MarginPersen,
    decimal BiayaTambahanItem,
    decimal TaxPersen,
    decimal DiskonDefaultPersen,
    bool IsActive);
