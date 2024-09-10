using Bilreg.Application.AdmisiContext.LayananSub.LayananDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Options;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananDkAgg;
using Nuna.Lib.DataAccessHelper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.LayananDkAgg
{
    public class LayananDkDal : ILayananDkDal
    {
        private readonly DatabaseOptions _opt;

        public LayananDkDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }
        public LayananDkModel GetData(ILayananDkKey key)
        {
            const string sql = @"
                SELECT
                    fs_kd_layanan_dk, fs_nm_layanan_dk, fn_rawat_inap,
                    fn_rawat_jalan, fn_kesehatan_jiwa, fn_bedah,
                    fn_rujukan, fn_kunj_rumah, fn_layanan_sub
                FROM 
                    ta_layanan_dk
                WHERE
                    FS_KD_LAYANAN_DK = @fs_kd_layanan_dk
                ";
            var dp = new DynamicParameters();
            dp.AddParam("@fs_kd_layanan_dk", key.LayananDkId, SqlDbType.VarChar);

            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            return conn.ReadSingle<LayananDkDto>(sql, dp);
        }

        public IEnumerable<LayananDkModel> ListData()
        {
            const string sql = @"
                SELECT
                    fs_kd_layanan_dk, fs_nm_layanan_dk, fn_rawat_inap,
                    fn_rawat_jalan, fn_kesehatan_jiwa, fn_bedah,
                    fn_rujukan, fn_kunj_rumah, fn_layanan_sub
                FROM 
                    ta_layanan_dk ";
            using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
            return conn.Read<LayananDkDto>(sql);
        }
    }
    public class LayananDkDalTest
    {
        private readonly LayananDkDal _sut;

        public LayananDkDalTest()
        {
            _sut= new LayananDkDal(ConnStringHelper.GetTestEnv());
        }
        [Fact]
        public void GetTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new LayananDkModel("01", "Penyakit Dalam", 1, 1, 1, 0, 1, 1, 1);

            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }
        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new LayananDkModel("01", "Penyakit Dalam", 1, 1, 1, 0, 1, 1, 1);

            var actual = _sut.ListData();
            actual.Should().ContainEquivalentOf(expected);
        }
    }

    public class LayananDkDto : LayananDkModel
    {
        public LayananDkDto() : base(
            string.Empty, string.Empty, 0, 0, 0, 0, 0, 0, 0)
        {
        }
        public string fs_kd_layanan_dk { get => LayananDkId; set => LayananDkId = value; }
        public string fs_nm_layanan_dk { get => LayananDkName; set => LayananDkName = value; }
        public int fn_rawat_inap  { get => RawatInapCode; set => RawatInapCode = value; }
        public int fn_rawat_jalan  { get => RawatJalanCode; set => RawatJalanCode = value; }
        public int fn_kesehatan_jiwa  { get => KesehatanJiwaCode; set => KesehatanJiwaCode = value; }
        public int fn_bedah  { get => BedahCode; set => BedahCode = value; }
        public int fn_rujukan { get => RujukanCode; set => RujukanCode = value; }
        public int fn_kunj_rumah  { get => KunjunganRumahCode; set => KunjunganRumahCode = value; }
        public int fn_layanan_sub { get => LayananSubCode; set => LayananSubCode = value; }

    }
}
