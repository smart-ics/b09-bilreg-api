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

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.CaraMasukDkAgg
{
    public class CaraMasukDkDal : ICaraMasukDkDal
    {
        private readonly DatabaseOptions _opt;

        public CaraMasukDkDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        // Menghapus metode Delete dan Insert karena data readonly
        public CaraMasukDkModel GetData(ICaraMasukDkKey key)
        {
            const string sql = @"
            SELECT fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk
            FROM ta_cara_masuk_dk
            WHERE fs_kd_cara_masuk_dk = @fs_kd_cara_masuk_dk";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_cara_masuk_dk", key.CaraMasukDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.ReadSingle<CaraMasukDkDto>(sql, dp);
            return result?.ToModel();
        }

        public IEnumerable<CaraMasukDkModel> ListData()
        {
            const string sql = @"
            SELECT fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk
            FROM ta_cara_masuk_dk";

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<CaraMasukDkDto>(sql);
            return result?.Select(x => x.ToModel());
        }
    }


    public class CaraMasukDkDto
        {
            public string fs_kd_cara_masuk_dk { get; set; }
            public string fs_nm_cara_masuk_dk { get; set; }
            public CaraMasukDkModel ToModel() => CaraMasukDkModel.Create(fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk);
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
            var testData = CaraMasukDkModel.Create("9", "KUNJUNGAN RUMAH");

            // ACT
            var actual = _sut.GetData(testData);

            // ASSERT
            actual.Should().BeEquivalentTo(testData);
        }

        [Fact]
        public void ListDataTest()
        {
            
            // ACT
            var actual = _sut.ListData().ToList();

            // ASSERT
            actual.Should().Contain(x => x.CaraMasukDkId == "8" && x.CaraMasukDkName == "DATANG SENDIRI");
            actual.Should().Contain(x => x.CaraMasukDkId == "9" && x.CaraMasukDkName == "KUNJUNGAN RUMAH");
        }
    }

}
