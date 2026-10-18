using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.PenjualanFeature;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// Kontrak akses data langsung tabel legacy tb_trs_dobill_umum (68 kolom).
/// Mendukung partisipasi transaksi SQL atomik dan filter query aktif soft-void.
/// </summary>
public interface IPenjualanDal
{
    void Insert(PenjualanDto dto, SqlConnection? conn = null, SqlTransaction? tx = null);
    void Update(PenjualanDto dto, SqlConnection? conn = null, SqlTransaction? tx = null);
    void SoftVoid(string penjualanId, string tglVoid, string jamVoid, string userVoid, string reason, SqlConnection? conn = null, SqlTransaction? tx = null);
    void Delete(IPenjualanKey key, SqlConnection? conn = null, SqlTransaction? tx = null);
    PenjualanDto? GetData(IPenjualanKey key, SqlConnection? conn = null, SqlTransaction? tx = null);
    IEnumerable<PenjualanDto> ListByReg(string regId);
    IEnumerable<PenjualanDto> ListWorklist(string tglTrs);
    IEnumerable<PenjualanDto> ListByResep(string resepId);
}
