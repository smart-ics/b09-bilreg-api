using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialFeature;
using Bilreg.Domain.PasienContext;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.StatusSosialFeature;

public class SukuDal : ISukuDal
{
    private readonly DatabaseOptions _opt;

    public SukuDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(SukuType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_suku(fs_kd_suku, fs_nm_suku)
            VALUES 
                (@fs_kd_suku, @fs_nm_suku)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", model.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_suku", model.SukuName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SukuType model)
    {
        const string sql = @"
            UPDATE ta_suku
            SET fs_nm_suku = @fs_nm_suku
            WHERE fs_kd_suku = @fs_kd_suku";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", model.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_suku", model.SukuName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISukuKey key)
    {
        const string sql = @"
            DELETE FROM ta_suku
            WHERE fs_kd_suku = @fs_kd_suku";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", key.SukuId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<SukuType> GetData(ISukuKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_suku AS SukuId, 
                fs_nm_suku AS SukuName
            FROM ta_suku
            WHERE fs_kd_suku = @fs_kd_suku";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", key.SukuId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<SukuType>(sql, dp));
    }

    public MayBe<IEnumerable<SukuType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_suku AS SukuId, 
                fs_nm_suku AS SukuName
            FROM  ta_suku";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<SukuType>(sql));
    }

}
