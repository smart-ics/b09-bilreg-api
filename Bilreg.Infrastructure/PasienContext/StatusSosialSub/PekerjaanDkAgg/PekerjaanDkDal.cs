using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.StatusSosialSub.PekerjaanDkAgg;

public class PekerjaanDkDal : IPekerjaanDkDal
{
    private readonly DatabaseOptions _opt;

    public PekerjaanDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PekerjaanDkModel model)
    {
        //  QUERY
        const string sql = @"
                INSERT INTO ta_pekerjaan_dk(fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk)
                VALUES (@fs_kd_pekerjaan_dk, @fs_nm_pekerjaan_dk)";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pekerjaan_dk", model.PekerjaanDkName, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PekerjaanDkModel model)
    {
        //  QUERY
        const string sql = @"
                UPDATE ta_pekerjaan_dk
                SET fs_nm_pekerjaan_dk = @fs_nm_pekerjaan_dk
                WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pekerjaan_dk", model.PekerjaanDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pekerjaan_dk", model.PekerjaanDkName, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPekerjaanDkKey key)
    {
        //  QUERY
        const string sql = @"
                DELETE FROM ta_pekerjaan_dk
                WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pekerjaan_dk", key.PekerjaanDkId, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PekerjaanDkModel GetData(IPekerjaanDkKey key)
    {
        const string sql = @"
                SELECT fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk
                FROM ta_pekerjaan_dk
                WHERE fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_pekerjaan_dk", key.PekerjaanDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PekerjaanDkDto>(sql, dp);
    }

    public IEnumerable<PekerjaanDkModel> ListData()
    {
        const string sql = @"
                SELECT fs_kd_pekerjaan_dk, fs_nm_pekerjaan_dk
                FROM ta_pekerjaan_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PekerjaanDkDto>(sql);
    }
}

internal class PekerjaanDkDto : PekerjaanDkModel
{
    public string fs_kd_pekerjaan_dk { get => PekerjaanDkId; set => PekerjaanDkId = value; }
    public string fs_nm_pekerjaan_dk { get => PekerjaanDkName; set => PekerjaanDkName = value; }
    public PekerjaanDkDto() : base(string.Empty, string.Empty)
    {
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
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new PekerjaanDkModel("A", "B"));
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new PekerjaanDkModel("A", "B"));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new PekerjaanDkModel("A", "B"));
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PekerjaanDkModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GivenNotExistDate_ThenReturnNull()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PekerjaanDkModel("A", "B");
        var actual = _sut.GetData(expected);
        actual.Should().BeNull();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new List<PekerjaanDkModel> { new PekerjaanDkModel("A", "B") };
        _sut.Insert(new PekerjaanDkModel("A", "B"));
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(expected);
    }
}