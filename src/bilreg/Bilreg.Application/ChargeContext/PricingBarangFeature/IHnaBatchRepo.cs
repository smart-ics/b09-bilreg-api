using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature;

/// <summary>
/// Port persistensi transaksi batch penerbitan HNA (tabel legacy <c>tb_trs_hna</c> dan
/// <c>tb_trs_hna2</c>) beserta pemeliharaan <i>effective-dating</i> snapshot HNA aktif.
/// </summary>
public interface IHnaBatchRepo :
    ISaveChange<HnaBatchModel>
{
    /// <summary>
    /// Mengembalikan nilai <c>fn_hna_gen</c> aktif (baris dengan <c>fd_tgl_expired = '3000-01-01'</c>,
    /// belum void, dan sudah berlaku) untuk suatu barang pada tanggal transaksi tertentu.
    /// Mengembalikan <c>null</c> bila tidak ada snapshot HNA aktif.
    /// </summary>
    decimal? GetActiveHna(string barangId, DateTime tgl);
}
