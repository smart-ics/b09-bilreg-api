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
    
    public GetDataResult<CaraBayarDkModel> GetData2(ICaraBayarDkKey key)
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
        var data = conn.ReadSingle<CaraBayarDkModel>(sql, dp);
        var result = new GetDataResult<CaraBayarDkModel>(data, key.CaraBayarDkId);
        return result;
    }

    public ListDataResult<CaraBayarDkModel> ListData2()
    {
        // QUERY
        const string sql = @"
            SELECT 
                fs_kd_cara_bayar_dk AS CaraBayarDkId, 
                fs_nm_cara_bayar_dk AS CaraBayarDkName
            FROM 
                ta_cara_bayar_dk";

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<CaraBayarDkModel>(sql);
        var result = new ListDataResult<CaraBayarDkModel>(list);
        return result;
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
        var expected = new CaraBayarDkModel("A", "B");

        // ACT
        var actual = _sut.GetData2(expected).Value;
        
        // ASSERT
        actual.Should().BeNull();
    }

    [Fact]
    public void GivenEmptyData_ThenReturnNull_Test()
    {
        // ARRANGE
        using var trans = TransHelper.NewScope();
        var exepected = new CaraBayarDkModel("1", "Membayar Sendiri");
        // ACT
        var actual = _sut.ListData2().Value;
        
        // ASSERT
        var actualFirst = actual.First(x => x.CaraBayarDkId == "1");
        actualFirst.Should().BeEquivalentTo(exepected);
    }
}