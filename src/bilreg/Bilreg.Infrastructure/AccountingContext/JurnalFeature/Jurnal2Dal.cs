using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AccountingContext.JurnalFeature;

public interface IJurnal2Dal :
    IInsertBulk<Jurnal2Dto>,
    IDelete<IJurnalKey>,
    IListData<Jurnal2Dto, IJurnalKey>
{
}

public class Jurnal2Dal : IJurnal2Dal
{
    private readonly DatabaseOptions _opt;

    public Jurnal2Dal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<Jurnal2Dto> models)
    {
        const string sql = """
            INSERT INTO t_jurnal_dtl (
                fs_kd_jurnal, fn_urut,
                fs_kd_rek, fs_uraian, fn_jurnald, fn_jurnalk,
                fs_kd_unit, fs_kd_jk,
                fs_string00, fs_string01, fs_string02, fs_string03,
                fs_string04, fs_string05, fs_string06, fs_string07,
                fs_string08, fs_string09, fs_string10, fs_string11,
                fn_nilai_jasa, fn_nilai_obat
            )
            VALUES (
                @fs_kd_jurnal, @fn_urut,
                @fs_kd_rek, @fs_uraian, @fn_jurnald, @fn_jurnalk,
                @fs_kd_unit, @fs_kd_jk,
                @fs_string00, @fs_string01, @fs_string02, @fs_string03,
                @fs_string04, @fs_string05, @fs_string06, @fs_string07,
                @fs_string08, @fs_string09, @fs_string10, @fs_string11,
                @fn_nilai_jasa, @fn_nilai_obat
            )
            """;

        var listJurnal2 = models.ToList();

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();

        var result = conn.Execute(sql, listJurnal2.Select(item => new
        {
            fs_kd_jurnal = item.fs_kd_jurnal,
            fn_urut  = item.fn_urut,
            fs_kd_rek  = item.fs_kd_rek,
            fs_uraian  = item.fs_uraian,
            fn_jurnald  = item.fn_jurnald,
            fn_jurnalk  = item.fn_jurnalk,
            fs_kd_unit  = item.fs_kd_unit,
            fs_kd_jk = item.fs_kd_jk,
            fs_string00 = item.fs_string00,
            fs_string01 = item.fs_string01,
            fs_string02 = item.fs_string02,
            fs_string03 = item.fs_string03,
            fs_string04 = item.fs_string04,
            fs_string05 = item.fs_string05,
            fs_string06 = item.fs_string06,
            fs_string07 = item.fs_string07,
            fs_string08 = item.fs_string08,
            fs_string09 = item.fs_string09,
            fs_string10 = item.fs_string10,
            fs_string11 = item.fs_string11,
            fn_nilai_jasa = item.fn_nilai_jasa,
            fn_nilai_obat = item.fn_nilai_obat
        }));
    }

    public void Delete(IJurnalKey key)
    {
        const string sql = """
            DELETE FROM t_jurnal_dtl
            WHERE fs_kd_jurnal = @fs_kd_jurnal
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jurnal", key.JurnalId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<Jurnal2Dto> ListData(IJurnalKey key)
    {
        const string sql = """
            SELECT
                fs_kd_jurnal, fn_urut,
                fs_kd_rek, fs_uraian, fn_jurnald, fn_jurnalk,
                fs_kd_unit, fs_kd_jk,
                fs_string00, fs_string01, fs_string02, fs_string03,
                fs_string04, fs_string05, fs_string06, fs_string07,
                fs_string08, fs_string09, fs_string10, fs_string11,
                fn_nilai_jasa, fn_nilai_obat
            FROM t_jurnal_dtl
            WHERE fs_kd_jurnal = @fs_kd_jurnal
            ORDER BY fn_urut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jurnal", key.JurnalId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<Jurnal2Dto>(sql, dp);
    }
}