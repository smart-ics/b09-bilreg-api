using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanFeature.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature.CaraBayarDkAgg;

public class CaraBayarDkDal : ICaraBayarDkDal
{
    private readonly DatabaseOptions _opt;

    public CaraBayarDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    
    public CaraBayarDkType GetData(ICaraBayarDkKey key)
    {
        // QUERY
        const string sql = @"
             SELECT 
                 fs_kd_cara_bayar_dk AS CaraBayarDkId, 
                 fs_nm_cara_bayar_dk AS CaraBayarDkName
             FROM 
                 ta_cara_bayar_dk
             WHERE 
                 fs_kd_cara_bayar_dk = @fs_kd_cara_bayar_dk";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_cara_bayar_dk", key.CaraBayarDkId, SqlDbType.VarChar);

        // EXECUTE
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<CaraBayarDkType>(sql, dp);

    }

    public IEnumerable<CaraBayarDkType> ListData()
    {
        // QUERY
        const string sql = @"
             SELECT 
                 fs_kd_cara_bayar_dk AS CaraBayarDkId, 
                 fs_nm_cara_bayar_dk AS CaraBayarDkName
             FROM 
                 ta_cara_bayar_dk";

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<CaraBayarDkType>(sql);
        
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
        var expected = new CaraBayarDkType("A", "B");

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
        var exepected = new CaraBayarDkType("1", "Membayar Sendiri");
        // ACT
        var actual = _sut.ListData();

        // ASSERT
        var actualFirst = actual.First(x => x.CaraBayarDkId == "1");
        actualFirst.Should().BeEquivalentTo(exepected);
    }
}