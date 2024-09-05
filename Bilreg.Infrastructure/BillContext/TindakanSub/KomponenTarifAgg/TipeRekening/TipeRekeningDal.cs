using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRekening;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using System.Data.SqlClient;
using System.Data;
using Xunit;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.KomponenTarifAgg.TipeRekening
{
    public class TipeRekeningDal : ITipeRekeningDal
    {
        private readonly DatabaseOptions _opt;

        public TipeRekeningDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public TipeRekeningModel GetData(ITipeRekeningKey key)
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
            dp.AddParam("@fs_kd_rek_tipe", key.TipeRekeningId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            return conn.ReadSingle<TipeRekeningDto>(sql, dp);
        }

        public IEnumerable<TipeRekeningModel> ListData()
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
            return conn.Read<TipeRekeningDto>(sql);
        }
    }

    public class TipeRekeningDalTest
    {
      private readonly TipeRekeningDal _sut;
        public TipeRekeningDalTest()
        {
            _sut = new TipeRekeningDal(ConnStringHelper.GetTestEnv());
        }
        [Fact]
        public void GetTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeRekeningModel("AKM DPS", "Akumulasi Depresiasi", true, 13, "K");

            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }
        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeRekeningModel("AKM DPS", "Akumulasi Depresiasi", true, 13, "K");

            var actual = _sut.ListData();
            actual.Should().ContainEquivalentOf(expected);
        }
    }

    public class TipeRekeningDto : TipeRekeningModel
    {
        public TipeRekeningDto() : base(string.Empty, string.Empty, true, decimal.Zero, string.Empty)
        {
        }
        public string fs_kd_rek_tipe { get => TipeRekeningId; set => TipeRekeningId = value; }
        public string fs_nm_rek_tipe { get => TipeRekeningName; set => TipeRekeningName = value; }
        public bool fb_neraca { get => IsNeraca; set => IsNeraca = value; } 
        public decimal fn_urut { get => NoUrut; set => NoUrut = value; }
        public string fs_dk { get => DebetKredit; set => DebetKredit = value; }

    }
}
