using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

// ReSharper disable InconsistentNaming
namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature.TipeJaminanAgg;
public interface ITipeJaminanDal :
    IInsert<TipeJaminanDto>,
    IUpdate<TipeJaminanDto>,
    IDelete<ITipeJaminanKey>,
    IGetData<TipeJaminanDto, ITipeJaminanKey>,
    IListData<TipeJaminanDto>
{
}
public class TipeJaminanDal : ITipeJaminanDal
{
    private readonly DatabaseOptions _opt;

    public TipeJaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TipeJaminanDto dto)
    {
        const string sql = """
             INSERT INTO ta_tipe_jaminan(
                 fs_kd_tipe_jaminan, fs_nm_tipe_jaminan, 
                 fb_aktif, fs_kd_jaminan)
             VALUES(
                 @fs_kd_tipe_jaminan, @fs_nm_tipe_jaminan, 
                 @fb_aktif, @fs_kd_jaminan)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tipe_jaminan", dto.fs_nm_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_jaminan", dto.fs_kd_jaminan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeJaminanDto dto)
    {
        const string sql = """
            UPDATE
                ta_tipe_jaminan
            SET
                fs_nm_tipe_jaminan = @fs_nm_tipe_jaminan, 
                fb_aktif = @fb_aktif, 
                fs_kd_jaminan = @fs_kd_jaminan
            WHERE
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tipe_jaminan", dto.fs_nm_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_jaminan", dto.fs_kd_jaminan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public void Delete(ITipeJaminanKey key)
    {
        const string sql = """
             DELETE FROM
                 ta_tipe_jaminan
             WHERE
                 fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan
             """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", key.TipeJaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeJaminanDto GetData(ITipeJaminanKey key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                aa.fb_aktif, aa.fs_kd_jaminan,
                ISNULL(bb.fs_nm_jaminan, '-') fs_nm_jaminan,
                ISNULL(bb.fs_kd_cara_bayar_dk, '-') fs_kd_cara_bayar_dk,
                ISNULL(cc.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk
            FROM
                ta_tipe_jaminan aa
                LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan 
                LEFT JOIN ta_cara_bayar_dk cc ON bb.fs_kd_cara_bayar_dk = cc.fs_kd_cara_bayar_dk
            WHERE
                aa.fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", key.TipeJaminanId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TipeJaminanDto>(sql, dp);
    }

    public IEnumerable<TipeJaminanDto> ListData()
    {
        const string sql = """
            SELECT
                aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                aa.fb_aktif, aa.fs_kd_jaminan,
                ISNULL(bb.fs_nm_jaminan, '-') fs_nm_jaminan,
                ISNULL(bb.fs_kd_cara_bayar_dk, '-') fs_kd_cara_bayar_dk,
                ISNULL(cc.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk
            FROM
                ta_tipe_jaminan aa
                LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan
                LEFT JOIN ta_cara_bayar_dk cc ON bb.fs_kd_cara_bayar_dk = cc.fs_kd_cara_bayar_dk
            WHERE
                aa.fb_aktif = 1
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeJaminanDto>(sql);
    }
}