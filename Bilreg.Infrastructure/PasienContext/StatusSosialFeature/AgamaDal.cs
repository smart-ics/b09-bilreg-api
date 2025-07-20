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

public class AgamaDal : IAgamaDal
{
    private readonly DatabaseOptions _opt;

    public AgamaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AgamaType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_agama(fs_kd_agama, fs_nm_agama)
            VALUES 
                (@fs_kd_agama, @fs_nm_agama)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_agama", model.AgamaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_agama", model.AgamaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AgamaType model)
    {
        const string sql = @"
            UPDATE ta_agama
            SET fs_nm_agama = @fs_nm_agama
            WHERE fs_kd_agama = @fs_kd_agama";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_agama", model.AgamaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_agama", model.AgamaName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAgamaKey key)
    {
        const string sql = @"
            DELETE FROM ta_agama
            WHERE fs_kd_agama = @fs_kd_agama";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_agama", key.AgamaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<AgamaType> GetData(IAgamaKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_agama AS fs_kd_agama, 
                fs_nm_agama AS fs_nm_agama
            FROM ta_agama
            WHERE fs_kd_agama = @fs_kd_agama";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_agama", key.AgamaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<AgamaType>(sql, dp));
    }

    public MayBe<IEnumerable<AgamaType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_agama AS AgamaId, 
                fs_nm_agama AS AgamaName
            FROM  ta_agama";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<AgamaType>(sql));
    }

}

public class AgamaDalTest
{
    private readonly AgamaDal _sut;

    public AgamaDalTest()
    {
        _sut = new AgamaDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new AgamaType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new AgamaType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new AgamaType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new AgamaType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new List<AgamaType> {new AgamaType("A", "B")};
        _sut.Insert(new AgamaType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().BeEquivalentTo(expected);
    }
}