using Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanSub;
using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;

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
        dp.AddParam("@fs_kd_jaminan", model.Jaminan.JaminanId, SqlDbType.VarChar);

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
        dp.AddParam("@fs_kd_jaminan", model.Jaminan.JaminanId, SqlDbType.VarChar);

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

    public GetDataResult<TipeJaminanModel> GetData2(ITipeJaminanKey key)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                aa.fb_aktif, aa.fs_kd_jaminan,
                ISNULL(bb.fs_nm_jaminan, '') fs_nm_jaminan,
                ISNULL(cc.fs_nm_grup_jaminan, '') fs_nm_grup_jaminan,
                ISNULL(dd.fs_nm_cara_bayar_dk, '') fs_nm_cara_bayar_dk
            FROM
                ta_tipe_jaminan aa
                LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan 
                LEFT JOIN ta_grup_jaminan cc ON bb.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
                LEFT JOIN ta_cara_bayar_dk dd ON bb.fS_kd_cara_bayar_dk = dd.fs_kd_cara_bayar_dk
            WHERE
                aa.fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_jaminan", key.TipeJaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var data = conn.ReadSingle<TipeJaminanDto>(sql, dp);
        var result = new GetDataResult<TipeJaminanModel>(data.ToModel(), key.TipeJaminanId);
        return result;
    }

    public ListDataResult<TipeJaminanModel> ListData2()
    {
        const string sql = @"
            SELECT
                aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                aa.fb_aktif, aa.fs_kd_jaminan,
                ISNULL(bb.fs_nm_jaminan, '') fs_nm_jaminan,
                ISNULL(cc.fs_nm_grup_jaminan, '') fs_nm_grup_jaminan,
                ISNULL(dd.fs_nm_cara_bayar_dk, '') fs_nm_cara_bayar_dk
            FROM
                ta_tipe_jaminan aa
                LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan 
                LEFT JOIN ta_grup_jaminan cc ON bb.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
                LEFT JOIN ta_cara_bayar_dk dd ON bb.fS_kd_cara_bayar_dk = dd.fs_kd_cara_bayar_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list =  conn.Read<TipeJaminanDto>(sql)?.ToList() ?? [];
        var result = new ListDataResult<TipeJaminanModel>(list.Select(x => x.ToModel()));
        return result;
    }

    public ListDataResult<TipeJaminanModel> ListData2(IJaminanKey filter)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                aa.fb_aktif, aa.fs_kd_jaminan,
                ISNULL(bb.fs_nm_jaminan, '') fs_nm_jaminan,
                ISNULL(cc.fs_nm_grup_jaminan, '') fs_nm_grup_jaminan,
                ISNULL(dd.fs_nm_cara_bayar_dk, '') fs_nm_cara_bayar_dk
            FROM
                ta_tipe_jaminan aa
                LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan 
                LEFT JOIN ta_grup_jaminan cc ON bb.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
                LEFT JOIN ta_cara_bayar_dk dd ON bb.fS_kd_cara_bayar_dk = dd.fs_kd_cara_bayar_dk 
            WHERE
                aa.fs_kd_jaminan = @fs_kd_jaminan ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", filter.JaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list =  conn.Read<TipeJaminanDto>(sql, dp)?.ToList() ?? [];
        var result = new ListDataResult<TipeJaminanModel>(list.Select(x => x.ToModel()));
        return result;;
    }
}

public class TipeJaminanDto
{
    public string fs_kd_tipe_jaminan { get; set; }
    public string fs_nm_tipe_jaminan { get; set; }
    public bool fb_aktif { get; set; }
    public string fs_kd_jaminan { get; set; }
    public string fs_nm_jaminan { get; set; }
    public string fs_nm_cara_bayar_dk { get; set; }
    public string fs_nm_grup_jaminan { get; set; }

    public TipeJaminanModel ToModel()
    {
        var jaminan = new JaminanViewType(
            fs_kd_jaminan, fs_nm_jaminan,
            fs_nm_cara_bayar_dk, fs_nm_grup_jaminan);
        var tipeJaminan = new TipeJaminanModel(
            fs_kd_tipe_jaminan, fs_nm_tipe_jaminan, fb_aktif,
            jaminan);
        return tipeJaminan;
    }
}