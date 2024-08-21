using Bilreg.Application.AdmPasienContext.AgamaContext;
using Bilreg.Domain.AdmPasienContext.AgamaAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmPasienContext.AgamaAgg
{
    public class AgamaDal : IAgamaDal
    {
        private readonly DatabaseOptions _opt;

        public AgamaDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public void Insert(AgamaModel model)
        {
            //  QUERY
            const string sql = @"
                INSERT INTO ta_agama (fs_kd_agama, fs_nm_agama)
                VALUES (@fs_kd_agama, @fs_nm_agama)";

            //  PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_agama", model.AgamaId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_agama", model.AgamaName, SqlDbType.VarChar);

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Update(AgamaModel model)
        {
            //  QUERY
            const string sql = @"
                UPDATE  ta_agama 
                SET fs_nm_agama = @fs_nm_agama
                WHERE fs_kd_agama = @fs_kd_agama";

            //  PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_agama", model.AgamaId, SqlDbType.VarChar);
            dp.AddParam("@fs_nm_agama", model.AgamaName, SqlDbType.VarChar);

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public void Delete(IAgamaKey key)
        {
            //  QUERY
            const string sql = @"
                DELETE FROM ta_agama
                WHERE fs_kd_agama = @fs_kd_agama";

            //  PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_agama", key.AgamaId, SqlDbType.VarChar);

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            conn.Execute(sql, dp);
        }

        public AgamaModel GetData(IAgamaKey key)
        {
            //  QUERY
            const string sql = @"
                SELECT fs_kd_agama, fs_nm_agama
                FROM ta_agama
                WHERE fs_kd_agama = @fs_kd_agama";

            //  PARAM
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_agama", key.AgamaId, SqlDbType.VarChar);

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.ReadSingle<AgamaDto>(sql, dp);
            return result?.ToModel()!;
        }

        public IEnumerable<AgamaModel> ListData()
        {
            //  QUERY
            const string sql = @"
                SELECT fs_kd_agama, fs_nm_agama
                FROM ta_agama ";

            //  EXECUTE
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<AgamaDto>(sql);
            return result?.Select(x => x.ToModel())!;
        }

        public class AgamaDto
        {
            public string fs_kd_agama { get; set; }
            public string fs_nm_agama { get; set; }

            public AgamaModel ToModel()
            {
                return AgamaModel.Create(fs_kd_agama, fs_nm_agama);
            }
        }
    }

    public class AgamaDalTest
    {
        private readonly AgamaDal _sut;
        
        public AgamaDalTest()
        {
            _sut = new AgamaDal(ConnStringHelper.GetTestEnv());
        }
        
        [Fact]
        public void InsertTest()
        {
            using var trans = TransHelper.NewScope();
            var expeted = AgamaModel.Create("A", "B");
            _sut.Insert(expeted);
        }
        [Fact]
        public void UpdateTest()
        {
            using var trans = TransHelper.NewScope();
            var expeted = AgamaModel.Create("A", "B");
            _sut.Update(expeted);
        }
        [Fact]
        public void DeleteTest()
        {
            using var trans = TransHelper.NewScope();
            var expeted = AgamaModel.Create("A", "B");
            _sut.Delete(expeted);
        }

        [Fact]
        public void GetDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expeted = AgamaModel.Create("A", "B");
            _sut.Insert(expeted);
            var actual = _sut.GetData(expeted);
            actual.Should().BeEquivalentTo(expeted);
        }
        
        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expeted = AgamaModel.Create("A", "B");
            _sut.Insert(expeted);
            var actual = _sut.ListData();
            actual.Should().BeEquivalentTo(new List<AgamaModel>{expeted});
        }
    }
}
