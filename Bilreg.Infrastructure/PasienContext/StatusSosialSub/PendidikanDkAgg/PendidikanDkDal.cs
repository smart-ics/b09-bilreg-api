using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.StatusSosialSub.PendidikanDkAgg;

public class PendidikanDkDal: IPendidikanDkDal
{
    private readonly DatabaseOptions _opt;

    public PendidikanDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PendidikanDkModel model)
    {
        // QUERY
        const string sql = @"
            INSERT INTO ta_pendidikan_dk(fs_kd_pendidikan_dk, fs_nm_pendidikan_dk)
            VALUES(@fs_kd_pendidikan_dk, @fs_nm_pendidikan_dk)";
        
        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", model.PendidikanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pendidikan_dk", model.PendidikanDkName, SqlDbType.VarChar);
        
        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PendidikanDkModel model)
    {
        // QUERY
        const string sql = @"
            UPDATE ta_pendidikan_dk
            SET fs_nm_pendidikan_dk = @fs_nm_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";
        
        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", model.PendidikanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pendidikan_dk", model.PendidikanDkName, SqlDbType.VarChar);
        
        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPendidikanDkKey key)
    {
        // QUERY
        const string sql = @"
            DELETE FROM ta_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";
        
        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", key.PendidikanDkId, SqlDbType.VarChar);
        
        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PendidikanDkModel GetData(IPendidikanDkKey key)
    {
        // QUERY
        const string sql = @"
            SELECT * FROM ta_pendidikan_dk
            WHERE fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk";
        
        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pendidikan_dk", key.PendidikanDkId, SqlDbType.VarChar);
        
        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<PendidikanDkDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<PendidikanDkModel> ListData()
    {
        // QUERY
        const string sql = @"
            SELECT fs_kd_pendidikan_dk, fs_nm_pendidikan_dk
            FROM ta_pendidikan_dk";
        
        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<PendidikanDkDto>(sql);
        return result?.Select(x => x.ToModel())!;
    }
}

public class PendidikanDkDto
{
    public string fs_kd_pendidikan_dk { get; set; }
    public string fs_nm_pendidikan_dk { get; set; }
    
    public PendidikanDkModel ToModel() => PendidikanDkModel.Create(fs_kd_pendidikan_dk, fs_nm_pendidikan_dk);
}

public class PendidikanDkDalTest
{
    private readonly PendidikanDkDal _sut;

    public PendidikanDkDalTest()
    {
        _sut = new PendidikanDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(PendidikanDkModel.Create("A", "B"));
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(PendidikanDkModel.Create("A", "B"));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(PendidikanDkModel.Create("A", "B"));
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = PendidikanDkModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GivenNonExistData_ThenReturnNull_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = PendidikanDkModel.Create("A", "B");
        var actual = _sut.GetData(expected);
        actual.Should().BeNull();
    }
}