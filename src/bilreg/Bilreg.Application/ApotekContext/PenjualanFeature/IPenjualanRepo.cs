using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.PenjualanFeature;

/// <summary>
/// Kontrak repository untuk agregat PenjualanModel (DU...) di tabel tb_trs_dobill_umum & tb_trs_dobill_umum2.
/// Menyediakan operasi persistensi atomik, pembacaan dokumen, query registrasi, dan worklist operasional.
/// </summary>
public interface IPenjualanRepo :
    ISaveChange<PenjualanModel>,
    ILoadEntity<PenjualanModel, IPenjualanKey>
{
    /// <summary>
    /// Mengambil seluruh riwayat transaksi penjualan aktif untuk suatu nomor registrasi pasien.
    /// </summary>
    IReadOnlyList<PenjualanModel> ListByReg(string regId);

    /// <summary>
    /// Mengambil daftar transaksi penjualan apotek pada tanggal bisnis tertentu untuk worklist operasional.
    /// </summary>
    IReadOnlyList<PenjualanModel> ListWorklist(DateOnly businessDate);

    /// <summary>
    /// Mengambil transaksi penjualan yang terkait dengan suatu nomor e-resep (idempotency check).
    /// </summary>
    IReadOnlyList<PenjualanModel> ListByResep(string resepId);
}
