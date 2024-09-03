using Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.TipeJaminanAgg;

public class TipeJaminanDal : ITipeJaminanDal
{
    private readonly DatabaseOptions _opt;

    public TipeJaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TipeJaminanModel model)
    {
        const string sql = @"
                INSERT INTO ta_tipe_jaminan(
                    fs_kd_tipe_jaminan, fs_nm_tipe_jaminan, 
                    fb_aktif, fs_kd_jaminan)
                VALUES(
                    @fs_kd_tipe_jaminan, @fs_nm_tipe_jaminan, 
                    @fb_aktif, @fs_kd_jaminan)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar); 
        dp.AddParam("@fs_nm_tipe_jaminan", model.TipeJaminanName, SqlDbType.VarChar); 
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeJaminanModel model)
    {
        const string sql = @"
                UPDATE
                    ta_tipe_jaminan
                SET
                    fs_nm_tipe_jaminan = @fs_nm_tipe_jaminan, 
                    fb_aktif = @fb_aktif, 
                    fs_kd_jaminan = @fs_kd_jaminan
                WHERE
                    fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tipe_jaminan", model.TipeJaminanName, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeJaminanKey key)
    {
        const string sql = @"
                DELETE FROM
                    ta_tipe_jaminan
                WHERE
                    fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", key.TipeJaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeJaminanModel GetData(ITipeJaminanKey key)
    {
        const string sql = @"
                SELECT
                    aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                    aa.fb_aktif, aa.fs_kd_jaminan,
                    ISNULL(bb.fs_nm_jaminan, '') fs_nm_jaminan
                FROM
                    ta_tipe_jaminan aa
                    LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan
                WHERE
                    fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", key.TipeJaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TipeJaminanDto>(sql, dp);
    }

    public IEnumerable<TipeJaminanModel> ListData()
    {
        const string sql = @"
                SELECT
                    aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                    aa.fb_aktif, aa.fs_kd_jaminan,
                    ISNULL(bb.fs_nm_jaminan, '') fs_nm_jaminan
                FROM
                    ta_tipe_jaminan aa
                    LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan ";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeJaminanDto>(sql);
    }

    public IEnumerable<TipeJaminanModel> ListData(IJaminanKey filter)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                aa.fb_aktif, aa.fs_kd_jaminan,
                ISNULL(bb.fs_nm_jaminan, '') fs_nm_jaminan
            FROM
                ta_tipe_jaminan aa
                LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan 
            WHERE
                aa.fs_kd_jaminan = @fs_kd_jaminan ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", filter.JaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeJaminanDto>(sql, dp);
    }
}

public class TipeJaminanDto : TipeJaminanModel
{
    public TipeJaminanDto() : base(string.Empty, string.Empty)
    {
    }

    public string fs_kd_tipe_jaminan { get => TipeJaminanId; set => TipeJaminanId = value; }
    public string fs_nm_tipe_jaminan { get => TipeJaminanName; set => TipeJaminanName = value; }
    public bool fb_aktif { get => IsAktif; set => IsAktif = value; }
    public string fs_kd_jaminan { get => JaminanId; set => JaminanId = value; }
}