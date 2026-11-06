using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Data access for the <c>ta_trs_merge</c> registration merge relation table.
/// </summary>
public interface IBilrgTrsMergeDal
{
    void Insert(BilrgTrsMergeDto model);

    bool Exists(string mergeRegId, string asalRegId);

    IEnumerable<string> ListAsalRegId(string mergeRegId);

    string? GetMergeRegId(string asalRegId);
}

public class BilrgTrsMergeDal : IBilrgTrsMergeDal
{
    private readonly DatabaseOptions _opt;

    public BilrgTrsMergeDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BilrgTrsMergeDto model)
    {
        const string sql = """
            INSERT INTO ta_trs_merge (
                fs_kd_reg_merge, fs_kd_reg_asal,
                fs_crt_user, fd_crt_date, fs_upd_user, fd_upd_date)
            VALUES (
                @MergeRegId, @AsalRegId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;

        var dp = BuildParams(model);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public bool Exists(string mergeRegId, string asalRegId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM ta_trs_merge
            WHERE fs_kd_reg_merge = @MergeRegId
              AND fs_kd_reg_asal = @AsalRegId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@MergeRegId", mergeRegId, SqlDbType.VarChar);
        dp.AddParam("@AsalRegId", asalRegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var count = conn.ExecuteScalar<int>(sql, dp);
        return count > 0;
    }

    public IEnumerable<string> ListAsalRegId(string mergeRegId)
    {
        const string sql = """
            SELECT fs_kd_reg_asal
            FROM ta_trs_merge
            WHERE fs_kd_reg_merge = @MergeRegId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@MergeRegId", mergeRegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<string>(sql, dp);
    }

    public string? GetMergeRegId(string asalRegId)
    {
        const string sql = """
            SELECT fs_kd_reg_merge
            FROM ta_trs_merge
            WHERE fs_kd_reg_asal = @AsalRegId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AsalRegId", asalRegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<string>(sql, dp);
    }

    private static DynamicParameters BuildParams(BilrgTrsMergeDto model)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@MergeRegId", model.MergeRegId, SqlDbType.VarChar);
        dp.AddParam("@AsalRegId", model.AsalRegId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", model.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", model.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", model.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", model.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}
