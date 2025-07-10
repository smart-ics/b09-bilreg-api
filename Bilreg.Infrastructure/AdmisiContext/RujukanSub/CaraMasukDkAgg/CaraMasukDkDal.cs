using Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data.SqlClient;
using System.Data;
using FluentAssertions;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.CaraMasukDkAgg;

public class CaraMasukDkDal : ICaraMasukDkDal
{
    private readonly DatabaseOptions _opt;

    public CaraMasukDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public GetDataResult<CaraMasukDkModel> GetData2(ICaraMasukDkKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk
            FROM 
                ta_cara_masuk_dk
            WHERE 
                fs_kd_cara_masuk_dk = @fs_kd_cara_masuk_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_cara_masuk_dk", key.CaraMasukDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var data = conn.ReadSingle<CaraMasukDkDto>(sql, dp);
        var result = new GetDataResult<CaraMasukDkModel>(data.ToModel(), key.CaraMasukDkId);
        return result;
    }

    public ListDataResult<CaraMasukDkModel> ListData2()
    {
        const string sql = @"
            SELECT 
                fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk
            FROM 
                ta_cara_masuk_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<CaraMasukDkDto>(sql);
        var result = new ListDataResult<CaraMasukDkModel>(list?.Select(x => x.ToModel()).ToList());
        return result;
    }
}


public class CaraMasukDkDto
{
    public string fs_kd_cara_masuk_dk { get; set; }
    public string fs_nm_cara_masuk_dk { get; set; }
    public CaraMasukDkModel ToModel() => new CaraMasukDkModel(fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk);
}

public class CaraMasukDkDalTest
{
    private readonly CaraMasukDkDal _sut;

    public CaraMasukDkDalTest()
    {
        _sut = new CaraMasukDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void GetDataTest()
    {
        // ARRANGE
        var testData = new CaraMasukDkModel("9", "KUNJUNGAN RUMAH");

        // ACT
        var actual = _sut.GetData2(testData).Value;

        // ASSERT
        actual.Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void ListDataTest()
    {
            
        // ACT
        var actual = _sut.ListData2().Value;

        // ASSERT
        actual.Should().Contain(x => x.CaraMasukDkId == "8" && x.CaraMasukDkName == "DATANG SENDIRI");
        actual.Should().Contain(x => x.CaraMasukDkId == "9" && x.CaraMasukDkName == "KUNJUNGAN RUMAH");
    }
}