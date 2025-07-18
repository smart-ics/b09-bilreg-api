using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialFeature.PendidikanDkAgg;
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

public class PendidikanDkDal : IPendidikanDkDal
{
    private readonly DatabaseOptions _opt;

    public PendidikanDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PendidikanDkType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_pendidikan_dk(fs_kd_pendidikan_dk, fs_nm_pendidikan_dk)
            VALUES 
                (@fs_kd_pendidikan_dk, @fs_nm_pendidikan_dk)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", model.PendidikanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pendidikan_dk", model.PendidikanDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PendidikanDkType model)
    {
        const string sql = @"
            UPDATE ta_pendidikan_dk
            SET fs_nm_pendidikan_dk = @fs_nm_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", model.PendidikanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pendidikan_dk", model.PendidikanDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPendidikanDkKey key)
    {
        const string sql = @"
            DELETE FROM ta_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", key.PendidikanDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<PendidikanDkType> GetData(IPendidikanDkKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_pendidikan_dk AS PendidikanDkId, 
                fs_nm_pendidikan_dk AS PendidikanDkName
            FROM ta_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", key.PendidikanDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<PendidikanDkType>(sql, dp));
    }

    public MayBe<IEnumerable<PendidikanDkType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_pendidikan_dk AS PendidikanDkId, 
                fs_nm_pendidikan_dk AS PendidikanDkName
            FROM  ta_pendidikan_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<PendidikanDkType>(sql));
    }

}

public class PendidikanDkDalTest
{
    private readonly PendidikanDkDal _sut;

    public PendidikanDkDalTest()
    {
        _sut = new PendidikanDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new PendidikanDkType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new PendidikanDkType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new PendidikanDkType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PendidikanDkType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new List<PendidikanDkType> {new PendidikanDkType("A", "B")};
        _sut.Insert(new PendidikanDkType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().BeEquivalentTo(expected);
    }
}