using Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using System.Data;
using System.Data.SqlClient;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.TipeJaminanAgg;

public class TipeJaminanDal : ITipeJaminanDal
{
    private readonly DatabaseOptions _opt;

    public TipeJaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TipeJaminanType model)
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
    public void Update(TipeJaminanType model)
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

    public MayBe<TipeJaminanType> GetData(ITipeJaminanKey key)
    {
        const string sql = @"
                 SELECT
                     aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                     aa.fb_aktif, aa.fs_kd_jaminan,
                     ISNULL(bb.fs_nm_jaminan, '-') fs_nm_jaminan,
                     ISNULL(cc.fs_nm_grup_jaminan, '-') fs_nm_grup_jaminan,
                     ISNULL(dd.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk
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
        return MayBe
            .From(conn.ReadSingle<TipeJaminanDto>(sql, dp))
            .Map(x => x.ToModel());
    }

    public MayBe<IEnumerable<TipeJaminanType>> ListData()
    {
        const string sql = @"
                 SELECT
                     aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                     aa.fb_aktif, aa.fs_kd_jaminan,
                     ISNULL(bb.fs_nm_jaminan, '-') fs_nm_jaminan,
                     ISNULL(cc.fs_nm_grup_jaminan, '-') fs_nm_grup_jaminan,
                     ISNULL(dd.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk
                 FROM
                     ta_tipe_jaminan aa
                     LEFT JOIN ta_jaminan bb ON aa.fs_kd_jaminan = bb.fs_kd_jaminan 
                     LEFT JOIN ta_grup_jaminan cc ON bb.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
                     LEFT JOIN ta_cara_bayar_dk dd ON bb.fS_kd_cara_bayar_dk = dd.fs_kd_cara_bayar_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.Read<TipeJaminanDto>(sql))
            .Map(x => x.Select(y => y.ToModel()));
        return datas;
    }

    public MayBe<IEnumerable<TipeJaminanType>> ListData(IJaminanKey filter)
    {
        const string sql = @"
                 SELECT
                     aa.fs_kd_tipe_jaminan, aa.fs_nm_tipe_jaminan, 
                     aa.fb_aktif, aa.fs_kd_jaminan,
                     ISNULL(bb.fs_nm_jaminan, '-') fs_nm_jaminan,
                     ISNULL(cc.fs_nm_grup_jaminan, '-') fs_nm_grup_jaminan,
                     ISNULL(dd.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk
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
        var datas = MayBe
            .From(conn.Read<TipeJaminanDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
        return datas;
    }
    
}

public class TipeJaminanDto
{
    public string fs_kd_tipe_jaminan { get ; set; } 
    public string fs_nm_tipe_jaminan { get; set; }
    public bool fb_aktif { get; set; }
    public string fs_kd_jaminan { get; set; }
    public string fs_nm_jaminan { get; set; }
    public string fs_nm_cara_bayar_dk { get; set; }
    public string fs_nm_grup_jaminan { get; set; }

    public TipeJaminanType ToModel()
    {
        var jaminan = new JaminanReff(fs_kd_jaminan, fs_nm_jaminan);
        var tipeJaminan = new TipeJaminanType(
            fs_kd_tipe_jaminan, fs_nm_tipe_jaminan, fb_aktif,
            jaminan);
        return tipeJaminan;
    }
}

public class TipeJaminanDalTest
{
    private readonly TipeJaminanDal _sut;
    public TipeJaminanDalTest()
    {
        _sut = new TipeJaminanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeJaminanType("A", "B", true, JaminanType.Default.ToReff());
        _sut.Insert(expected);
    }

    [Fact]
    public void UT2_UpdatTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeJaminanType("A", "B", true, JaminanType.Default.ToReff());
        _sut.Update(expected);
    }

    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var key = new TipeJaminanType("A", "B", true, JaminanType.Default.ToReff());
        _sut.Delete(key);
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeJaminanType("A", "B", true, JaminanType.Default.ToReff());
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeJaminanType("A", "B", true, JaminanType.Default.ToReff());
        _sut.Insert(expected);
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
    [Fact]
    public void UT6_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeJaminanType("A", "B", true, JaminanType.Default.ToReff());
        _sut.Insert(expected);

        var actual = _sut.ListData(JaminanType.Key("-")).Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}