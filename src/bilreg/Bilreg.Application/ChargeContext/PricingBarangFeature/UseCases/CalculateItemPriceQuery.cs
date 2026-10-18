using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Query simulasi/evaluasi kalkulasi harga jual satuan suatu barang untuk tipe harga tertentu
/// pada tanggal transaksi tertentu (ARCHITECTURE TD-06).
/// </summary>
/// <param name="BarangId">Kode barang persediaan.</param>
/// <param name="TipeHargaId">Kode tipe harga / tipe penjamin.</param>
/// <param name="TransactionDate">Tanggal transaksi untuk pembacaan snapshot HNA aktif.</param>
/// <param name="Qty">Kuantitas yang diminta.</param>
/// <param name="SatuanId">Satuan yang diminta; kosong berarti satuan dasar/terkecil.</param>
public record CalculateItemPriceQuery(
    string BarangId,
    string TipeHargaId,
    DateTime TransactionDate,
    int Qty = 1,
    string SatuanId = "") : IRequest<ItemPriceCalculationResultDto>;
