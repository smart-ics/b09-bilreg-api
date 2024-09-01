using Bilreg.Application.BillContext.RoomChargeSub.KelasDkAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuna.Lib.DataAccessHelper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.RoomChargeSub.KelasDkAgg
{
    public class KelasDkDal : IKelasDkDal
    {
        private readonly DatabaseOptions _opt;

        public KelasDkDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public class KelasDkDto
        {
            public string fs_kd_kelas_dk { get; set; }
            public string fs_nm_kelas_dk { get; set; }
            public KelasDkModel ToModel() => KelasDkModel.Create(fs_kd_kelas_dk, fs_nm_kelas_dk);
        }

        public KelasDkModel GetData(IKelasDkKey key)
        {
            const string sql = @"
            SELECT fs_kd_kelas_dk,fs_nm_kelas_dk
            FROM ta_kelas_dk
            WHERE fs_kd_kelas_dk = @fs_kd_kelas_dk
            ";

            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_kelas_dk", key.KelasDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            KelasDkDto result = conn.ReadSingle<KelasDkDto>(sql, dp);
            return result?.ToModel();

        }

        public IEnumerable<KelasDkModel> ListData()
        {
            const string sql = @"
            SELECT fs_kd_kelas_dk, fs_nm_kelas_dk
            FROM ta_kelas_dk
            ";
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            var result = conn.Read<KelasDkDto>(sql);
            return result?.Select(x=>x.ToModel());
        }
    }

    public class KelasDkDalTest
    {
        private readonly IKelasDkDal _sut;
        public KelasDkDalTest()
        {
            _sut = new KelasDkDal(ConnStringHelper.GetTestEnv());
        }

        [Fact]
        public void GetDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = KelasDkModel.Create("1", "VVIP");
            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = KelasDkModel.Create("1", "VVIP");
            var actual = _sut.ListData();
            var actualFirst = actual.First(x => x.KelasDkId == "1");
            actualFirst.Should().BeEquivalentTo(expected);
        }
    }
}
