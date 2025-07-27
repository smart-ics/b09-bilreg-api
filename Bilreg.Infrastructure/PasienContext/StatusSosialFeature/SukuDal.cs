using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialFeature;
using Bilreg.Domain.PasienContext;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.StatusSosialFeature;

public class SukuDal : ISukuDal
{
    private readonly DatabaseOptions _opt;

    public SukuDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(SukuType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_suku(fs_kd_suku, fs_nm_suku)
            VALUES 
                (@fs_kd_suku, @fs_nm_suku)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", model.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_suku", model.SukuName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SukuType model)
    {
        const string sql = @"
            UPDATE ta_suku
            SET fs_nm_suku = @fs_nm_suku
            WHERE fs_kd_suku = @fs_kd_suku";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", model.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_suku", model.SukuName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISukuKey key)
    {
        const string sql = @"
            DELETE FROM ta_suku
            WHERE fs_kd_suku = @fs_kd_suku";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", key.SukuId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<SukuType> GetData(ISukuKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_suku AS SukuId, 
                fs_nm_suku AS SukuName
            FROM ta_suku
            WHERE fs_kd_suku = @fs_kd_suku";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", key.SukuId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<SukuType>(sql, dp));
    }

    public MayBe<IEnumerable<SukuType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_suku AS SukuId, 
                fs_nm_suku AS SukuName
            FROM  ta_suku";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<SukuType>(sql));
    }

}

public class SukuDalTest
{
    private readonly SukuDal _sut;

    public SukuDalTest()
    {
        _sut = new SukuDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new SukuType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new SukuType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new SukuType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new SukuType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new SukuType("A", "B");
        _sut.Insert(new SukuType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}