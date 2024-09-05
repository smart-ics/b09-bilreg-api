using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRek;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg.TipeRek;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using System.Data.SqlClient;
using System.Data;
using Xunit;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.TindakanAgg.TipeRek
{
    public class TipeRekDal : ITipeRekDal
    {
        private readonly DatabaseOptions _opt;

        public TipeRekDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public TipeRekModel GetData(ITipeRekKey key)
        {
            const string sql = @"
            SELECT
                FS_KD_REK_TIPE,
                FS_NM_REK_TIPE,
                FB_NERACA,
                FN_URUT,
                FS_DK
            FROM 
                T_REK_TIPE
            WHERE
                FS_KD_REK_TIPE = @fs_kd_rek_tipe;
        ";
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_rek_tipe", key.TipeRekId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            return conn.ReadSingle<TipeRekDto>(sql, dp);
        }

        public IEnumerable<TipeRekModel> ListData()
        {
            const string sql = @"
            SELECT
                FS_KD_REK_TIPE,
                FS_NM_REK_TIPE,
                FB_NERACA,
                FN_URUT,
                FS_DK
            FROM
                T_REK_TIPE
        ";
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            return conn.Read<TipeRekDto>(sql);
        }
    }

    public class TipeRekDalTest
    {
        private readonly TipeRekDal _sut;
        public TipeRekDalTest()
        {
            _sut = new TipeRekDal(ConnStringHelper.GetTestEnv());
        }
        [Fact]
        public void GetTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeRekModel("AKM DPS", "Akumulasi Depresiasi", true, 13, "K");

            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }
        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeRekModel("AKM DPS", "Akumulasi Depresiasi", true, 13, "K");

            var actual = _sut.ListData();
            actual.Should().ContainEquivalentOf(expected);
        }
    }

    public class TipeRekDto : TipeRekModel
    {
        public TipeRekDto() : base(string.Empty, string.Empty, true, decimal.Zero, string.Empty)
        {
        }
        public string fs_kd_rek_tipe { get => TipeRekId; set => TipeRekId = value; }
        public string fs_nm_rek_tipe { get => TipeRekName; set => TipeRekName = value; }
        public bool fb_neraca { get => IsNeraca; set => IsNeraca = value; }
        public decimal fn_urut { get => NoUrut; set => NoUrut = value; }
        public string fs_dk { get => DebetKredit; set => DebetKredit = value; }

    }
}
