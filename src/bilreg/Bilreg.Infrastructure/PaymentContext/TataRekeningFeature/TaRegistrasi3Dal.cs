using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public interface ITaRegistrasi3Dal :
    IDelete<IRegKey>,
    IListData<TaRegistrasi3Dto, IRegKey>,
    IInsertBulk<TaRegistrasi3Dto>
{
}

public class TaRegistrasi3Dal : ITaRegistrasi3Dal
{
    private readonly DatabaseOptions _opt;

    public TaRegistrasi3Dal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Delete(IRegKey key)
    {
        const string sql = """
            DELETE FROM
                ta_registrasi3
            WHERE
                fs_kd_reg = @RegId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TaRegistrasi3Dto> ListData(IRegKey key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_reg,
                aa.fs_kd_bayar,
                ISNULL(bb.fs_nm_tipe_jaminan, '') AS fs_nm_bayar,
                aa.fn_jasa,
                aa.fn_obat,
                aa.fs_kd_rek,
                ISNULL(bb.fb_subsidi, 0) AS fb_subsidi
            FROM
                ta_registrasi3 aa
                LEFT JOIN ta_tipe_jaminan bb ON aa.fs_kd_bayar = bb.fs_kd_tipe_jaminan
            WHERE
                aa.fs_kd_reg = @RegId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TaRegistrasi3Dto>(sql, dp);
    }

    public void Insert(IEnumerable<TaRegistrasi3Dto> models)
    {
        const string sql = """
            INSERT INTO ta_registrasi3(
                fs_kd_reg, fs_kd_bayar, fn_jasa, fn_obat, fs_kd_rek)
            VALUES(
                @fs_kd_reg, @fs_kd_bayar, @fn_jasa, @fn_obat, @fs_kd_rek)
            """;

        var list = models.ToList();
        if (list.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        conn.Execute(sql, list.Select(item => new
        {
            fs_kd_reg = item.fs_kd_reg,
            fs_kd_bayar = item.fs_kd_bayar,
            fn_jasa = item.fn_jasa,
            fn_obat = item.fn_obat,
            fs_kd_rek = item.fs_kd_rek
        }));
    }
}
