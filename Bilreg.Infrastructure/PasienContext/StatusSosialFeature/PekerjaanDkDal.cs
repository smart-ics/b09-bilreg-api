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

public class PekerjaanDkDal : IPekerjaanDkDal
{
    private readonly DatabaseOptions _opt;

    public PekerjaanDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PekerjaanDkType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_pekerjaan_dk(fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk)
            VALUES 
                (@fs_kd_pekerjaan_dk, @fs_nm_pekerjaan_dk)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pekerjaan_dk", model.PekerjaanDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PekerjaanDkType model)
    {
        const string sql = @"
            UPDATE ta_pekerjaan_dk
            SET fs_nm_pekerjaan_dk = @fs_nm_pekerjaan_dk
            WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pekerjaan_dk", model.PekerjaanDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPekerjaanDkKey key)
    {
        const string sql = @"
            DELETE FROM ta_pekerjaan_dk
            WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pekerjaan_dk", key.PekerjaanDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<PekerjaanDkType> GetData(IPekerjaanDkKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_pekerjaan_dk AS PekerjaanDkId, 
                fs_nm_pekerjaan_dk AS PekerjaanDkName
            FROM ta_pekerjaan_dk
            WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pekerjaan_dk", key.PekerjaanDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<PekerjaanDkType>(sql, dp));
    }

    public MayBe<IEnumerable<PekerjaanDkType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_pekerjaan_dk AS PekerjaanDkId, 
                fs_nm_pekerjaan_dk AS PekerjaanDkName
            FROM  ta_pekerjaan_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<PekerjaanDkType>(sql));
    }

}

public class PekerjaanDkDalTest
{
    private readonly PekerjaanDkDal _sut;

    public PekerjaanDkDalTest()
    {
        _sut = new PekerjaanDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new PekerjaanDkType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new PekerjaanDkType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new PekerjaanDkType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PekerjaanDkType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PekerjaanDkType("A", "B");
        _sut.Insert(new PekerjaanDkType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}