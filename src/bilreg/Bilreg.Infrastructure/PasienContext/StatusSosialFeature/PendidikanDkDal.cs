using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.StatusSosialFeature;

public class PendidikanDkDal : IPendidikanDkDal
{
    private readonly DatabaseOptions _opt;

    public PendidikanDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PendidikanDkType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_pendidikan_dk(fs_kd_pendidikan_dk, fs_nm_pendidikan_dk)
            VALUES 
                (@fs_kd_pendidikan_dk, @fs_nm_pendidikan_dk)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", model.PendidikanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pendidikan_dk", model.PendidikanDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PendidikanDkType model)
    {
        const string sql = @"
            UPDATE ta_pendidikan_dk
            SET fs_nm_pendidikan_dk = @fs_nm_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", model.PendidikanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pendidikan_dk", model.PendidikanDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPendidikanDkKey key)
    {
        const string sql = @"
            DELETE FROM ta_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", key.PendidikanDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<PendidikanDkType> GetData(IPendidikanDkKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_pendidikan_dk AS PendidikanDkId, 
                fs_nm_pendidikan_dk AS PendidikanDkName
            FROM ta_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", key.PendidikanDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<PendidikanDkType>(sql, dp));
    }

    public MayBe<IEnumerable<PendidikanDkType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_pendidikan_dk AS PendidikanDkId, 
                fs_nm_pendidikan_dk AS PendidikanDkName
            FROM  ta_pendidikan_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<PendidikanDkType>(sql));
    }

}
