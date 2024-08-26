using Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
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

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public class InstalasiDkDal : IInstalasiDkDal
    {
        private readonly DatabaseOptions _opt;

        public InstalasiDkDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public void Insert(InstalasiDkModel model)
        {
            // QUERY
            const string sql = @"
                INSERT INTO ta_instalasi_dk(fs_kd_instalasi_dk, fs_nm_instalasi_dk)
                VALUES (@fs_kd_instalasi_dk, @fs_nm_instalasi_dk)";

            // PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi_dk", model.InstalasiDkId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_instalasi_dk", model.InstalasiDkName, SqlDbType.VarChar);

            // EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Update(InstalasiDkModel model)
        {
            // QUERY
            const string sql = @"
                UPDATE ta_instalasi_dk
                SET fs_nm_instalasi_dk = @fs_nm_instalasi_dk
                WHERE fs_kd_instalasi_dk = @fs_kd_instalasi_dk";

            // PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi_dk", model.InstalasiDkId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_instalasi_dk", model.InstalasiDkName, SqlDbType.VarChar);

            // EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Delete(IInstalasiDkKey key)
        {
            // QUERY
            const string sql = @"
                DELETE FROM ta_instalasi_dk
                WHERE fs_kd_instalasi_dk = @fs_kd_instalasi_dk";

            // PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi_dk", key.InstalasiDkId, SqlDbType.VarChar);

            // EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public InstalasiDkModel GetData(IInstalasiDkKey key)
        {
            const string sql = @"
                SELECT fs_kd_instalasi_dk, fs_nm_instalasi_dk
                FROM ta_instalasi_dk
                WHERE fs_kd_instalasi_dk = @fs_kd_instalasi_dk";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_instalasi_dk", key.InstalasiDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            InstalasiDkDto result = conn.ReadSingle<InstalasiDkDto>(sql, dp);
            return result?.ToModel();
        }

        public IEnumerable<InstalasiDkModel> ListData()
        {
            const string sql = @"
                SELECT fs_kd_instalasi_dk, fs_nm_instalasi_dk
                FROM ta_instalasi_dk";

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<InstalasiDkDto>(sql);
            return result?.Select(x => x.ToModel());
        }
    }

    public class InstalasiDkDto
    {
        public string fs_kd_instalasi_dk { get; set; }
        public string fs_nm_instalasi_dk { get; set; }
        public InstalasiDkModel ToModel() => InstalasiDkModel.Create(fs_kd_instalasi_dk, fs_nm_instalasi_dk);
    }

    public class InstalasiDkDalTest
    {
        private readonly InstalasiDkDal _sut;

        public InstalasiDkDalTest()
        {
            _sut = new InstalasiDkDal(ConnStringHelper.GetTestEnv());
        }

        [Fact]
        public void InsertTest()
        {
            using var trans = TransHelper.NewScope();
            _sut.Insert(InstalasiDkModel.Create("A", "B"));
        }

        [Fact]
        public void UpdateTest()
        {
            using var trans = TransHelper.NewScope();
            _sut.Update(InstalasiDkModel.Create("A", "B"));
        }

        [Fact]
        public void DeleteTest()
        {
            using var trans = TransHelper.NewScope();
            _sut.Delete(InstalasiDkModel.Create("A", "B"));
        }

        [Fact]
        public void GetDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = InstalasiDkModel.Create("A", "B");
            _sut.Insert(expected);
            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GivenNotExistDate_ThenReturnNull()
        {
            using var trans = TransHelper.NewScope();
            var expected = InstalasiDkModel.Create("A", "B");
            var actual = _sut.GetData(expected);
            actual.Should().BeNull();
        }

        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new List<InstalasiDkModel> { InstalasiDkModel.Create("A", "B") };
            _sut.Insert(InstalasiDkModel.Create("A", "B"));
            var actual = _sut.ListData();
            actual.Should().BeEquivalentTo(expected);
        }
    }

}
