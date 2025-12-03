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

public class StatusKawinDkDal : IStatusKawinDkDal
{
    private readonly DatabaseOptions _opt;

    public StatusKawinDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StatusKawinDkType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_status_kawin_dk(fs_kd_status_kawin_dk, fs_nm_status_kawin_dk)
            VALUES 
                (@fs_kd_status_kawin_dk, @fs_nm_status_kawin_dk)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawinDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_status_kawin_dk", model.StatusKawinDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(StatusKawinDkType model)
    {
        const string sql = @"
            UPDATE ta_status_kawin_dk
            SET fs_nm_status_kawin_dk = @fs_nm_status_kawin_dk
            WHERE fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawinDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_status_kawin_dk", model.StatusKawinDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IStatusKawinDkKey key)
    {
        const string sql = @"
            DELETE FROM ta_status_kawin_dk
            WHERE fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", key.StatusKawinDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<StatusKawinDkType> GetData(IStatusKawinDkKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_status_kawin_dk AS StatusKawinDkId, 
                fs_nm_status_kawin_dk AS StatusKawinDkName
            FROM ta_status_kawin_dk
            WHERE fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", key.StatusKawinDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<StatusKawinDkType>(sql, dp));
    }

    public MayBe<IEnumerable<StatusKawinDkType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_status_kawin_dk AS StatusKawinDkId, 
                fs_nm_status_kawin_dk AS StatusKawinDkName
            FROM  ta_status_kawin_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<StatusKawinDkType>(sql));
    }

}
