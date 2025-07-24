using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub;

public class PropinsiDal : IPropinsiDal
{
    private readonly DatabaseOptions _opt;

    public PropinsiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PropinsiType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_propinsi(fs_kd_propinsi, fs_nm_propinsi)
            VALUES 
                (@fs_kd_propinsi, @fs_nm_propinsi)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", model.PropinsiId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_propinsi", model.PropinsiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PropinsiType model)
    {
        const string sql = @"
            UPDATE ta_propinsi
            SET fs_nm_propinsi = @fs_nm_propinsi
            WHERE fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", model.PropinsiId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_propinsi", model.PropinsiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPropinsiKey key)
    {
        const string sql = @"
            DELETE FROM ta_propinsi
            WHERE fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", key.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<PropinsiType> GetData(IPropinsiKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_propinsi AS PropinsiId, 
                fs_nm_propinsi AS PropinsiName
            FROM ta_propinsi
            WHERE fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", key.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<PropinsiType>(sql, dp));
    }

    public MayBe<IEnumerable<PropinsiType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_propinsi AS PropinsiId, 
                fs_nm_propinsi AS PropinsiName
            FROM  ta_propinsi";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<PropinsiType>(sql));
    }

}

public class PropinsiDalTest
{
    private readonly PropinsiDal _sut;

    public PropinsiDalTest()
    {
        _sut = new PropinsiDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new PropinsiType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new PropinsiType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new PropinsiType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PropinsiType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PropinsiType("A", "B");
        _sut.Insert(new PropinsiType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}