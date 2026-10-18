using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Command penyimpanan (insert/update) master tipe harga penjamin beserta jasa kefarmasian
/// Tuslah <c>/r</c> (<c>BiayaPerObat_r</c>) dan Embalase <c>/R</c> (<c>BiayaPerResep_R</c>)
/// ke tabel legacy <c>tb_tipe_barang</c>.
/// </summary>
public record TipeHargaSaveCommand(
    string TipeHargaId,
    string TipeHargaName,
    decimal DefaultMarginPersen,
    decimal BiayaPerObat_r,
    decimal BiayaPerResep_R,
    decimal TaxPersen,
    decimal DiskonPersen,
    bool IsActive) : IRequest<TipeHargaSaveResponse>, ITipeHargaKey;

/// <summary>
/// Hasil penyimpanan master tipe harga.
/// </summary>
/// <param name="TipeHargaId">Kode tipe harga yang disimpan.</param>
/// <param name="IsNew">True bila kode tipe harga baru didaftarkan; false bila diperbarui.</param>
public record TipeHargaSaveResponse(string TipeHargaId, bool IsNew);
