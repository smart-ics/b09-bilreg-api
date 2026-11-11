using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Input satu baris varian harga per tipe penjamin yang diserahkan pada command pemutakhiran
/// parameter harga barang (<c>tb_barang_tipe</c>).
/// </summary>
public record PricingBarangVariantInput(
    string TipeHargaId,
    decimal HargaJualFix,
    decimal MarginPersen,
    decimal BiayaTambahanItem,
    decimal TaxPersen,
    decimal DiskonDefaultPersen,
    bool IsActive);

/// <summary>
/// Command pemutakhiran parameter harga jual barang persediaan: plafon HET dan metode generate
/// HNA (<c>tb_barang</c>) beserta matriks varian harga per tipe penjamin (<c>tb_barang_tipe</c>).
/// </summary>
public record PricingBarangUpdateCommand(
    string BarangId,
    decimal HetNominal,
    MetodeGenHnaEnum MetodeGenHna,
    IReadOnlyList<PricingBarangVariantInput> Variants,
    string UserId) : IRequest<PricingBarangUpdateResponse>, IPricingBarangKey;

/// <summary>
/// Hasil pemutakhiran parameter harga barang.
/// </summary>
/// <param name="BarangId">Kode barang yang dimutakhirkan.</param>
/// <param name="IsNew">True bila parameter harga barang dibuat baru; false bila diperbarui.</param>
/// <param name="VariantCount">Jumlah varian harga per tipe penjamin setelah pemutakhiran.</param>
/// <param name="HetNominal">Plafon HET yang tersimpan.</param>
public record PricingBarangUpdateResponse(
    string BarangId,
    bool IsNew,
    int VariantCount,
    decimal HetNominal);
