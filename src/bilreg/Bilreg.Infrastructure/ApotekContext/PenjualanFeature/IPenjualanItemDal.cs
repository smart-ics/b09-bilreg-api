using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.PenjualanFeature;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// Kontrak akses data baris detail penjualan farmasi pada tabel tb_trs_dobill_umum2 (54 kolom).
/// Mendukung SqlBulkCopy berkinerja tinggi dan partisipasi transaksi SQL atomik.
/// </summary>
public interface IPenjualanItemDal
{
    void InsertBulk(IEnumerable<PenjualanItemDto> items, SqlConnection? conn = null, SqlTransaction? tx = null);
    void SoftVoidItems(string penjualanId, string tglVoid, string jamVoid, string userVoid, SqlConnection? conn = null, SqlTransaction? tx = null);
    void Delete(IPenjualanKey key, SqlConnection? conn = null, SqlTransaction? tx = null);
    IEnumerable<PenjualanItemDto> ListData(IPenjualanKey filter, SqlConnection? conn = null, SqlTransaction? tx = null);
}
