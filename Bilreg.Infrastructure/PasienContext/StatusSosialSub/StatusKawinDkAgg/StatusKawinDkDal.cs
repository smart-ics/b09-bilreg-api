using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.StatusSosialSub.StatusKawinDkAgg;

public class StatusKawinDkDal : IStatusKawinDkDal
{
    private readonly DatabaseOptions _opt;

    public StatusKawinDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(StatusKawinDkModel model)
    {
        //  QUERY
        const string sql = @"
            INSERT INTO ta_status_kawin_dk 
                (fs_kd_status_kawin_dk, fs_nm_status_kawin_dk)
            VALUES 
                (@fs_kd_status_kawin_dk, @fs_nm_status_kawin_dk)";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawinDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_status_kawin_dk", model.StatusKawinDkName, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(StatusKawinDkModel model)
    {
        //  QUERY
        const string sql = @"
            UPDATE 
                ta_status_kawin_dk
            SET 
                fs_nm_status_kawin_dk = @fs_nm_status_kawin_dk
            WHERE 
                fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk;";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", model.StatusKawinDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_status_kawin_dk", model.StatusKawinDkName, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IStatusKawinDkKey key)
    {
        //  QUERY
        const string sql = @"
            DELETE FROM 
                ta_status_kawin_dk 
            WHERE 
                fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk;";
        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", key.StatusKawinDkId, SqlDbType.VarChar);
        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public StatusKawinDkModel GetData(IStatusKawinDkKey key)
    {
        const string sql = @"
            SELECT  
                fs_kd_status_kawin_dk AS StatusKawinDkId, 
                fs_nm_status_kawin_dk AS StatusKawinDkName
            FROM 
                ta_status_kawin_dk
            WHERE 
                fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk;";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_status_kawin_dk", key.StatusKawinDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<StatusKawinDkModel>(sql, dp);
        return result;
    }

    public IEnumerable<StatusKawinDkModel> ListData()
    {
        const string sql = @"
            SELECT  
                fs_kd_status_kawin_dk AS StatusKawinDkId, 
                fs_nm_status_kawin_dk AS StatusKawinDkName
            FROM 
                ta_status_kawin_dk ";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<StatusKawinDkModel>(sql);
        return result;
    }
}

public class StatusKawinDalTest
{
    private readonly StatusKawinDkDal _sut;

    public StatusKawinDalTest()
    {
        _sut = new StatusKawinDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expeted = new StatusKawinDkModel("A", "B");
        _sut.Insert(expeted);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new StatusKawinDkModel("A", "B");
        _sut.Update(expected);
    }
        
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new StatusKawinDkModel("A", "B");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new StatusKawinDkModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expeted = new StatusKawinDkModel("A", "B");
        _sut.Insert(expeted);
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(new List<StatusKawinDkModel> { expeted });
    }
}