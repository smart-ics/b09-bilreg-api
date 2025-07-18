using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialFeature;
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

public class StatusKawinDkDal : IStatusKawinDkDal
{
    private readonly DatabaseOptions _opt;

    public StatusKawinDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(StatusKawinDkType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_status_kawin_dk(fs_kd_status_kawin_dk, fs_nm_status_kawin_dk)
            VALUES 
                (@fs_kd_status_kawin_dk, @fs_nm_status_kawin_dk)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawinDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_status_kawin_dk", model.StatusKawinDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(StatusKawinDkType model)
    {
        const string sql = @"
            UPDATE ta_status_kawin_dk
            SET fs_nm_status_kawin_dk = @fs_nm_status_kawin_dk
            WHERE fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawinDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_status_kawin_dk", model.StatusKawinDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IStatusKawinDkKey key)
    {
        const string sql = @"
            DELETE FROM ta_status_kawin_dk
            WHERE fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", key.StatusKawinDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<StatusKawinDkType> GetData(IStatusKawinDkKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_status_kawin_dk AS StatusKawinDkId, 
                fs_nm_status_kawin_dk AS StatusKawinDkName
            FROM ta_status_kawin_dk
            WHERE fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", key.StatusKawinDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<StatusKawinDkType>(sql, dp));
    }

    public MayBe<IEnumerable<StatusKawinDkType>> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_status_kawin_dk AS StatusKawinDkId, 
                fs_nm_status_kawin_dk AS StatusKawinDkName
            FROM  ta_status_kawin_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<StatusKawinDkType>(sql));
    }

}

public class StatusKawinDkDalTest
{
    private readonly StatusKawinDkDal _sut;

    public StatusKawinDkDalTest()
    {
        _sut = new StatusKawinDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new StatusKawinDkType("A", "B"));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new StatusKawinDkType("A", "B"));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new StatusKawinDkType("A", "B"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new StatusKawinDkType("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new List<StatusKawinDkType> {new StatusKawinDkType("A", "B")};
        _sut.Insert(new StatusKawinDkType("A", "B"));
        var actual = _sut.ListData().Value;
        actual.Should().BeEquivalentTo(expected);
    }
}