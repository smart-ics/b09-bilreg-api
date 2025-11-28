using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub;

public class KotaDal : IKotaDal
{
    private readonly DatabaseOptions _opt;

    public KotaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KotaType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_kota(fs_kd_kota, fs_nm_kota)
            VALUES 
                (@fs_kd_kota, @fs_nm_kota)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", model.KotaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kota", model.KotaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KotaType model)
    {
        const string sql = @"
            UPDATE ta_kota
            SET fs_nm_kota = @fs_nm_kota
            WHERE fs_kd_kota = @fs_kd_kota";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", model.KotaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kota", model.KotaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKotaKey key)
    {
        const string sql = @"
            DELETE FROM ta_kota
            WHERE fs_kd_kota = @fs_kd_kota";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", key.KotaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<KotaType> GetData(IKotaKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_kota AS KotaId, 
                fs_nm_kota AS KotaName
            FROM ta_kota
            WHERE fs_kd_kota = @fs_kd_kota";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", key.KotaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<KotaType>(sql, dp));
    }

    public MayBe<IEnumerable<KotaType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_kota AS KotaId, 
                fs_nm_kota AS KotaName
            FROM  ta_kota";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<KotaType>(sql));
    }

}
