using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanFeature.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature.GrupJaminanAgg;

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

public class GrupJaminanDalTest
{
    private readonly GrupJaminanDal _sut;

    public GrupJaminanDalTest()
    {
        _sut = new GrupJaminanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Insert(expected);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Insert(expected);
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}