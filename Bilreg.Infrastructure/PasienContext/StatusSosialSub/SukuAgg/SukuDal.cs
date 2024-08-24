using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.StatusSosialSub.SukuAgg;

public class SukuDal : ISukuDal
{
    private readonly DatabaseOptions _opt;

    public SukuDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(SukuModel model)
    {
        //  QUERY
        const string sql = @"
                INSERT INTO ta_suku(fs_kd_suku, fs_nm_suku)
                VALUES (@fs_kd_suku, @fs_nm_suku)";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", model.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_suku", model.SukuName, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SukuModel model)
    {
        //  QUERY
        const string sql = @"
                UPDATE ta_suku
                SET fs_nm_suku = @fs_nm_suku
                WHERE fs_kd_suku = @fs_kd_suku";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", model.SukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_suku", model.SukuName, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISukuKey key)
    {
        //  QUERY
        const string sql = @"
                DELETE FROM ta_suku
                WHERE fs_kd_suku = @fs_kd_suku";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", key.SukuId, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public SukuModel GetData(ISukuKey key)
    {
        const string sql = @"
            SELECT  fs_kd_suku, fs_nm_suku
            FROM ta_suku
            WHERE fs_kd_suku = @fs_kd_suku";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_suku", key.SukuId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<SukuDto>(sql, dp);
        return result?.ToModel();
    }

    public IEnumerable<SukuModel> ListData()
    {
        const string sql = @"
            SELECT  fs_kd_suku, fs_nm_suku
            FROM ta_suku ";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<SukuDto>(sql);
        return result?.Select(x => x.ToModel());
    }
}

public class SukuDto
{
    public string fs_kd_suku { get; set; }
    public string fs_nm_suku { get; set; }
    public SukuModel ToModel() => SukuModel.Create(fs_kd_suku, fs_nm_suku);
}

public class SukuDalTest
{
    private readonly SukuDal _sut;

    public SukuDalTest()
    {
        _sut = new SukuDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(SukuModel.Create("A", "B"));
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(SukuModel.Create("A", "B"));
    }
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(SukuModel.Create("A", "B"));
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = SukuModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GivenNotExistDate_ThenReturnNull()
    {
        using var trans = TransHelper.NewScope();
        var expected = SukuModel.Create("A", "B");
        var actual = _sut.GetData(expected);
        actual.Should().BeNull();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new List<SukuModel> {SukuModel.Create("A", "B")};
        _sut.Insert(SukuModel.Create("A", "B"));
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(expected);
    }
}