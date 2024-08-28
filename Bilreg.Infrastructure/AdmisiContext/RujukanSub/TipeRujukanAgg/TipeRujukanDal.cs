using Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.TipeRujukanAgg;
public class TipeRujukanDal : ITipeRujukanDal
{
    private readonly DatabaseOptions _opt;

    public TipeRujukanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TipeRujukanModel model)
    {
        //  QUERY
        const string sql = @"
                INSERT INTO ta_rujukan_tipe(fs_kd_rujukan_tipe, fs_nm_rujukan_tipe)
                VALUES (@fs_kd_rujukan_tipe, @fs_nm_rujukan_tipe)";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan_tipe", model.TipeRujukanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rujukan_tipe", model.TipeRujukanName, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeRujukanModel model)
    {
        //  QUERY
        const string sql = @"
                UPDATE ta_rujukan_tipe
                SET fs_nm_rujukan_tipe = @fs_nm_rujukan_tipe
                WHERE fs_kd_rujukan_tipe = @fs_kd_rujukan_tipe";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan_tipe", model.TipeRujukanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rujukan_tipe", model.TipeRujukanName, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeRujukanKey key)
    {
        //  QUERY
        const string sql = @"
                DELETE FROM ta_rujukan_tipe
                WHERE fs_kd_rujukan_tipe = @fs_kd_rujukan_tipe";

        //  PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan_tipe", key.TipeRujukanId, SqlDbType.VarChar);

        //  EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeRujukanModel GetData(ITipeRujukanKey key)
    {
        const string sql = @"
                SELECT fs_kd_rujukan_tipe, fs_nm_rujukan_tipe
                FROM ta_rujukan_tipe
                WHERE fs_kd_rujukan_tipe = @fs_kd_rujukan_tipe";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rujukan_tipe", key.TipeRujukanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        TipeRujukanDto result = conn.ReadSingle<TipeRujukanDto>(sql, dp);
        return result?.ToModel();
    }

    public IEnumerable<TipeRujukanModel> ListData()
    {
        const string sql = @"
                SELECT fs_kd_rujukan_tipe, fs_nm_rujukan_tipe
                FROM ta_rujukan_tipe";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<TipeRujukanDto>(sql);
        return result?.Select(x => x.ToModel());
    }
}

public class TipeRujukanDto
{
    public string fs_kd_rujukan_tipe { get; set; }
    public string fs_nm_rujukan_tipe { get; set; }
    public TipeRujukanModel ToModel() => TipeRujukanModel.Create(fs_kd_rujukan_tipe, fs_nm_rujukan_tipe);
}

public class TipeRujukanDalTest
{
    private readonly TipeRujukanDal _sut;

    public TipeRujukanDalTest()
    {
        _sut = new TipeRujukanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(TipeRujukanModel.Create("A", "B"));
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(TipeRujukanModel.Create("A", "B"));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(TipeRujukanModel.Create("A", "B"));
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = TipeRujukanModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GivenNotExistDate_ThenReturnNull()
    {
        using var trans = TransHelper.NewScope();
        var expected = TipeRujukanModel.Create("A", "B");
        var actual = _sut.GetData(expected);
        actual.Should().BeNull();
    }

    [Fact]
    public void ListDataTest()
    {

        // ACT
        var actual = _sut.ListData().ToList();

        // ASSERT
        actual.Should().Contain(x => x.TipeRujukanId == "1" && x.TipeRujukanName == "DATANG SENDIRI");
        actual.Should().Contain(x => x.TipeRujukanId == "2" && x.TipeRujukanName == "BIDES");
    }
}

