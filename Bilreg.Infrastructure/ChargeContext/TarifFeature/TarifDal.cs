using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITarifDal :
    IGetData<TarifDto, ITarifKey>,
    IListData<TarifDto>,
    IListData<TarifDto, string>
{ }

public class TarifDal : ITarifDal
{
    private readonly DatabaseOptions _opt;

    public TarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public TarifDto GetData(ITarifKey key)
    {
        const string sql = @"
             SELECT 
                 a.fs_kd_tarif, a.fs_nm_tarif,
                 a.fs_kd_grup_tarif, a.fs_kd_grup_tarif_dk,
                 a.fs_kd_jenis_tarif, a.fs_kd_rekap_cetak_tarif,
                 ISNULL(b.fs_nm_grup_tarif,'') fs_nm_grup_tarif,
                 ISNULL(c.fs_nm_grup_tarif_dk,'') fs_nm_grup_tarif_dk,
                 ISNULL(d.fs_nm_jenis_tarif,'') fs_nm_jenis_tarif,
                 ISNULL(e.fs_nm_rekap_cetak_tarif, '') AS fs_nm_rekap_cetak_tarif
             FROM 
                 TA_TARIF a
                 LEFT JOIN ta_grup_tarif b ON a.fs_kd_grup_tarif = b.fs_kd_grup_tarif 
                 LEFT JOIN ta_grup_tarif_dk c ON a.fs_kd_grup_tarif_dk = c.fs_kd_grup_tarif_dk 
                 LEFT JOIN ta_jenis_tarif d ON a.fs_kd_jenis_tarif = d.fs_kd_jenis_tarif
                 LEFT JOIN ta_rekap_cetak_tarif e ON a.fs_kd_rekap_cetak_tarif = e.fs_kd_rekap_cetak_tarif
             WHERE
                 a.fs_kd_tarif = @fs_kd_tarif
                  ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif", key.TarifId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TarifDto>(sql, dp);
    }

    public IEnumerable<TarifDto> ListData()
    {
        const string sql = @"
             SELECT 
                 a.fs_kd_tarif, a.fs_nm_tarif, a.fs_kd_grup_tarif,
                 a.fs_kd_grup_tarif_dk, a.fs_kd_jenis_tarif, a.fs_kd_rekap_cetak_tarif,
                 ISNULL(b.fs_nm_grup_tarif,'') fs_nm_grup_tarif,
                 ISNULL(c.fs_nm_grup_tarif_dk,'') fs_nm_grup_tarif_dk,
                 ISNULL(d.fs_nm_jenis_tarif,'') fs_nm_jenis_tarif,
                 ISNULL(e.fs_nm_rekap_cetak_tarif, '') fs_nm_rekap_cetak_tarif
             FROM 
                 TA_TARIF a
                 LEFT JOIN ta_grup_tarif b ON a.fs_kd_grup_tarif = b.fs_kd_grup_tarif 
                 LEFT JOIN ta_grup_tarif_dk c ON a.fs_kd_grup_tarif_dk = c.fs_kd_grup_tarif_dk 
                 LEFT JOIN ta_jenis_tarif d ON a.fs_kd_jenis_tarif = d.fs_kd_jenis_tarif ";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifDto>(sql);
    }

    public IEnumerable<TarifDto> ListData(string keyword)
    {
        var filter = EscapeForContains(keyword);
        var sql = $"""
             SELECT 
                 a.fs_kd_tarif, 
                 a.fs_nm_tarif,
                 a.fs_kd_grup_tarif,
                 a.fs_kd_grup_tarif_dk,
                 a.fs_kd_jenis_tarif,
                 ISNULL(b.fs_nm_grup_tarif,'') fs_nm_grup_tarif,
                 ISNULL(c.fs_nm_grup_tarif_dk,'') fs_nm_grup_tarif_dk,
                 ISNULL(d.fs_nm_jenis_tarif,'') fs_nm_jenis_tarif
             FROM 
                 TA_TARIF a
                 LEFT JOIN ta_grup_tarif b ON a.fs_kd_grup_tarif = b.fs_kd_grup_tarif 
                 LEFT JOIN ta_grup_tarif_dk c ON a.fs_kd_grup_tarif_dk = c.fs_kd_grup_tarif_dk 
                 LEFT JOIN ta_jenis_tarif d ON a.fs_kd_jenis_tarif = d.fs_kd_jenis_tarif
             WHERE
                 CONTAINS(a.fs_nm_tarif, '{filter}')
                 AND a.FB_AKTIF = 1
         """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifDto>(sql);
    }

    private static string EscapeForContains(string term)
    {
        return "\"" + term.Replace("\"", "\"\"") + "*\"";
    }
}




