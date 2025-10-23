using Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
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
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.CaraMasukDkAgg;

public class CaraMasukDkDal : ICaraMasukDkDal
{
    private readonly DatabaseOptions _opt;

    public CaraMasukDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public MayBe<CaraMasukDkType> GetData(ICaraMasukDkKey key)
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
        return MayBe.From(conn.ReadSingle<CaraMasukDkType>(sql, dp));
    }

    public MayBe<IEnumerable<CaraMasukDkType>> ListData()
    {
        const string sql = @"
                 SELECT 
                     fs_kd_cara_masuk_dk AS CaraMasukDkId, 
                     fs_nm_cara_masuk_dk AS CaraMasukDkName
                 FROM 
                     ta_cara_masuk_dk";


        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<CaraMasukDkType>(sql));
    }
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
            var testData = new CaraMasukDkType("9", "KUNJUNGAN RUMAH");

            // ACT
            var actual = _sut.GetData(testData).Value;

            // ASSERT
            actual.Should().BeEquivalentTo(testData);
        }

        [Fact]
        public void ListDataTest()
        {

            // ACT
            var actual = _sut.ListData().Value;

            // ASSERT
            actual.Should().Contain(x => x.CaraMasukDkId == "8" && x.CaraMasukDkName == "DATANG SENDIRI");
            actual.Should().Contain(x => x.CaraMasukDkId == "9" && x.CaraMasukDkName == "KUNJUNGAN RUMAH");
        }
    }