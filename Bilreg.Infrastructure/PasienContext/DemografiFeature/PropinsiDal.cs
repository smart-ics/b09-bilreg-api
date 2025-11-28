using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub;

public class PropinsiDal : IPropinsiDal
{
    private readonly DatabaseOptions _opt;

    public PropinsiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PropinsiType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_propinsi(fs_kd_propinsi, fs_nm_propinsi)
            VALUES 
                (@fs_kd_propinsi, @fs_nm_propinsi)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", model.PropinsiId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_propinsi", model.PropinsiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PropinsiType model)
    {
        const string sql = @"
            UPDATE ta_propinsi
            SET fs_nm_propinsi = @fs_nm_propinsi
            WHERE fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", model.PropinsiId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_propinsi", model.PropinsiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPropinsiKey key)
    {
        const string sql = @"
            DELETE FROM ta_propinsi
            WHERE fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", key.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<PropinsiType> GetData(IPropinsiKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_propinsi AS PropinsiId, 
                fs_nm_propinsi AS PropinsiName
            FROM ta_propinsi
            WHERE fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", key.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<PropinsiType>(sql, dp));
    }

    public MayBe<IEnumerable<PropinsiType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_propinsi AS PropinsiId, 
                fs_nm_propinsi AS PropinsiName
            FROM  ta_propinsi";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<PropinsiType>(sql));
    }

}
