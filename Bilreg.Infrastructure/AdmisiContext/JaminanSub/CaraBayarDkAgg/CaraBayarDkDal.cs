using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public class CaraBayarDkDal: ICaraBayarDkDal
{
    private readonly DatabaseOptions _opt;

    public CaraBayarDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public CaraBayarDkModel GetData(ICaraBayarDkKey key)
    {
        // QUERY
        var sql = @"
            SELECT fs_kd_cara_bayar_dk, fs_nm_cara_bayar_dk
            FROM ta_cara_bayar_dk
            WHERE fs_kd_cara_bayar_dk = @fs_kd_cara_bayar_dk";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_cara_bayar_dk", key.CaraBayarDkId, SqlDbType.VarChar);
        
        // EXECUTE
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<CaraBayarDkDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<CaraBayarDkModel> ListData()
    {
        // QUERY
        var sql = @"
            SELECT fs_kd_cara_bayar_dk, fs_nm_cara_bayar_dk
            FROM ta_cara_bayar_dk";

        // EXECUTE
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<CaraBayarDkDto>(sql);
        return result?.Select(x => x.ToModel())!;
    }
}

public class CaraBayarDkDalTest
{
    private readonly CaraBayarDkDal _sut;

    public CaraBayarDkDalTest()
    {
        _sut = new CaraBayarDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void GivenNonExistData_ThenReturnNull_Test()
    {
        // ARRANGE
        using var trans = TransHelper.NewScope();
        var expected = CaraBayarDkModel.Create("A", "B");

        // ACT
        var actual = _sut.GetData(expected);
        
        // ASSERT
        actual.Should().BeNull();
    }

    [Fact]
    public void GivenEmptyData_ThenReturnNull_Test()
    {
        // ARRANGE
        using var trans = TransHelper.NewScope();
        var exepected = CaraBayarDkModel.Create("1", "Membayar Sendiri");
        // ACT
        var actual = _sut.ListData();
        
        // ASSERT
        var actualFirst = actual.First(x => x.CaraBayarDkId == "1");
        actualFirst.Should().BeEquivalentTo(exepected);
    }
}