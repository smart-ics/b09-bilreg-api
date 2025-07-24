using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub;

public class KotaDal : IKotaDal
{
    private readonly DatabaseOptions _opt;

    public KotaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KotaType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_kota(fs_kd_kota, fs_nm_kota)
            VALUES 
                (@fs_kd_kota, @fs_nm_kota)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", model.KotaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kota", model.KotaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KotaType model)
    {
        const string sql = @"
            UPDATE ta_kota
            SET fs_nm_kota = @fs_nm_kota
            WHERE fs_kd_kota = @fs_kd_kota";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", model.KotaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kota", model.KotaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKotaKey key)
    {
        const string sql = @"
            DELETE FROM ta_kota
            WHERE fs_kd_kota = @fs_kd_kota";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", key.KotaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<KotaType> GetData(IKotaKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_kota AS KotaId, 
                fs_nm_kota AS KotaName
            FROM ta_kota
            WHERE fs_kd_kota = @fs_kd_kota";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", key.KotaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<KotaType>(sql, dp));
    }

    public MayBe<IEnumerable<KotaType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_kota AS KotaId, 
                fs_nm_kota AS KotaName
            FROM  ta_kota";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<KotaType>(sql));
    }

}

public class KotaDalTest
{
    private readonly KotaDal _sut;

    public KotaDalTest()
    {
        _sut = new KotaDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new KotaType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new KotaType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new KotaType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KotaType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KotaType("A", "B");
        _sut.Insert(new KotaType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}