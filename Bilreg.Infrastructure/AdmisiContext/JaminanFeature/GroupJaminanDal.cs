using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanFeature.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public class GrupJaminanDal : IGroupJaminanDal
{
    private readonly DatabaseOptions _opt;

    public GrupJaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(GroupJaminanType model)
    {
        const string sql = @"
             INSERT INTO ta_grup_jaminan 
                 (fs_kd_grup_jaminan, fs_nm_grup_jaminan, fb_karyawan, fs_keterangan)
             VALUES 
                 (@fs_kd_grup_jaminan, @fs_nm_grup_jaminan, @fb_karyawan, @fs_keterangan)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", model.GroupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_jaminan", model.GroupJaminanName, SqlDbType.VarChar);
        dp.AddParam("@fb_karyawan", model.IsKaryawan, SqlDbType.Bit);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GroupJaminanType model)
    {
        const string sql = @"
             UPDATE 
                 ta_grup_jaminan
             SET 
                 fs_nm_grup_jaminan = @fs_nm_grup_jaminan,
                 fb_karyawan = @fb_karyawan,
                 fs_keterangan = @fs_keterangan
             WHERE 
                 fs_kd_grup_jaminan = @fs_kd_grup_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", model.GroupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_jaminan", model.GroupJaminanName, SqlDbType.VarChar);
        dp.AddParam("@fb_karyawan", model.IsKaryawan, SqlDbType.Bit);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGroupJaminanKey key)
    {
        const string sql = @"
             DELETE FROM 
                 ta_grup_jaminan
             WHERE 
                 fs_kd_grup_jaminan = @fs_kd_grup_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", key.GroupJaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<GroupJaminanType> GetData(IGroupJaminanKey key)
    {
        const string sql = @"
                 SELECT 
                     fs_kd_grup_jaminan AS GroupJaminanId, 
                     fs_nm_grup_jaminan AS GroupJaminanName, 
                     fb_karyawan AS IsKaryawan, 
                     fs_keterangan AS Keterangan
                 FROM 
                     ta_grup_jaminan
                 WHERE 
                     fs_kd_grup_jaminan = @fs_kd_grup_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", key.GroupJaminanId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.ReadSingle<GroupJaminanType>(sql, dp));
    }

    public MayBe<IEnumerable<GroupJaminanType>> ListData()
    {
        const string sql = @"
                 SELECT 
                     fs_kd_grup_jaminan AS GroupJaminanId, 
                     fs_nm_grup_jaminan AS GroupJaminanName, 
                     fb_karyawan AS IsKaryawan, 
                     fs_keterangan AS Keterangan
                 FROM 
                     ta_grup_jaminan ";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.Read<GroupJaminanType>(sql));
    }

}
