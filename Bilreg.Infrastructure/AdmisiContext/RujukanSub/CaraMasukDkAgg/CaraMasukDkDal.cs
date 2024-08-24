using Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
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

        public void Insert(CaraMasukDkModel model)
        {
            const string sql = @"
            INSERT INTO ta_cara_masuk_dk(fs_kd_cara_masuk_dk, fs_nm_cara_masuk_dk)
            VALUES (@fs_kd_cara_masuk_dk, @fs_nm_cara_masuk_dk)";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_cara_masuk_dk", model.CaraMasukDkId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_cara_masuk_dk", model.CaraMasukDkName, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Delete(string caraMasukDkId)
        {
            const string sql = @"
            DELETE FROM ta_cara_masuk_dk
            WHERE fs_kd_cara_masuk_dk = @fs_kd_cara_masuk_dk";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_cara_masuk_dk", caraMasukDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }


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
                //ARRANGE
                using var trans = TransHelper.NewScope();
                var testData = CaraMasukDkModel.Create("9", "KUNJUNGAN RUMAH");
                _sut.Delete(testData.CaraMasukDkId);
                _sut.Insert(testData);

                // ACT
                var actual = _sut.GetData(testData);

                // ASSERT
                actual.Should().BeEquivalentTo(testData);
            }



            [Fact]
            public void ListDataTest()
            {
                using var trans = TransHelper.NewScope();

                // Insert data baru
                var expected1 = CaraMasukDkModel.Create("A", "B");
                var expected2 = CaraMasukDkModel.Create("B", "A");
                _sut.Insert(expected1);
                _sut.Insert(expected2);

                // Ambil data
                var actual = _sut.ListData().ToList();

                // Assert
                actual.Should().Contain(x => x.CaraMasukDkId == "8" && x.CaraMasukDkName == "DATANG SENDIRI");
                actual.Should().Contain(x => x.CaraMasukDkId == "9" && x.CaraMasukDkName == "KUNJUNGAN RUMAH");
            }
        }
    }
}
