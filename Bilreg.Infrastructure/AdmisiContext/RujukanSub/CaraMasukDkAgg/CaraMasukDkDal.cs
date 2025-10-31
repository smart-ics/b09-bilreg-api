using Bilreg.Domain.AdmisiContext.RujukanSub;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data;
using System.Data.SqlClient;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.CaraMasukDkAgg;

public interface ICaraMasukDkDal :
    IGetData<CaraMasukDkType, ICaraMasukDkKey>,
    IListData<CaraMasukDkType>
{
}

public class CaraMasukDkDal : ICaraMasukDkDal
{
    private readonly DatabaseOptions _opt;

    public CaraMasukDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public CaraMasukDkType GetData(ICaraMasukDkKey key)
    {
        const string sql = @"
                 SELECT 
                     fs_kd_cara_masuk_dk AS CaraMasukDkId, 
                     fs_nm_cara_masuk_dk AS CaraMasukDkName
                 FROM 
                     ta_cara_masuk_dk
                 WHERE 
                     fs_kd_cara_masuk_dk = @CaraMasukDkId";

        var dp = new DynamicParameters();
        dp.AddParam("@CaraMasukDkId", key.CaraMasukDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<CaraMasukDkType>(sql, dp);
    }

    public IEnumerable<CaraMasukDkType> ListData()
    {
        const string sql = @"
                 SELECT 
                     fs_kd_cara_masuk_dk AS CaraMasukDkId, 
                     fs_nm_cara_masuk_dk AS CaraMasukDkName
                 FROM 
                     ta_cara_masuk_dk";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<CaraMasukDkType>(sql);
    }
}

public class CaraMasukDkDalTest
{
    private readonly CaraMasukDkDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ICaraMasukDkKey FakerKey()
        => CaraMasukDkType.Key("A");

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var actual = () => _sut.GetData(FakerKey());
        actual.Should().NotThrow<Exception>();
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var actual = () => _sut.ListData();
        actual.Should().NotThrow<Exception>();
    }
}