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

public class AgamaDal : IAgamaDal
{
    private readonly DatabaseOptions _opt;

    public AgamaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AgamaType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_agama(fs_kd_agama, fs_nm_agama)
            VALUES 
                (@fs_kd_agama, @fs_nm_agama)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_agama", model.AgamaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_agama", model.AgamaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AgamaType model)
    {
        const string sql = @"
            UPDATE ta_agama
            SET fs_nm_agama = @fs_nm_agama
            WHERE fs_kd_agama = @fs_kd_agama";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_agama", model.AgamaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_agama", model.AgamaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAgamaKey key)
    {
        const string sql = @"
            DELETE FROM ta_agama
            WHERE fs_kd_agama = @fs_kd_agama";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_agama", key.AgamaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<AgamaType> GetData(IAgamaKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_agama AS AgamaId, 
                fs_nm_agama AS AgamaName
            FROM ta_agama
            WHERE fs_kd_agama = @fs_kd_agama";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_agama", key.AgamaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<AgamaType>(sql, dp));
    }

    public MayBe<IEnumerable<AgamaType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_agama AS AgamaId, 
                fs_nm_agama AS AgamaName
            FROM  ta_agama";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<AgamaType>(sql));
    }

}
