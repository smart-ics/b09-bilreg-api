using Bilreg.Infrastructure.Helpers;
using Dapper;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Infrastructure.AdmisiContext.LayananFeature
{
    public interface ILayananDkDal :
        IGetData<LayananDkDto, ILayananDkKey>,
        IListData<LayananDkDto, IInstalasiDkKey>,
        IListData<LayananDkDto>
    { }
    public class LayananDkDal : ILayananDkDal
    {
        private readonly DatabaseOptions _opt;

        public LayananDkDal(IOptions<DatabaseOptions> opt)
        {
            _opt = opt.Value;
        }

        public LayananDkDto GetData(ILayananDkKey key)
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

        public IEnumerable<LayananDkDto> ListData(IInstalasiDkKey filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<LayananDkDto> ListData()
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
    //public class LayananDkDalTest
    //{
    //    private readonly LayananDkDal _sut;

    //    public LayananDkDalTest()
    //    {
    //        _sut = new LayananDkDal(ConnStringHelper.GetTestEnv());
    //    }
    //    [Fact]
    //    public void GetTest()
    //    {
    //        using var trans = TransHelper.NewScope();
    //        var expected = new LayananDkModel("01", "Penyakit Dalam", 1, 1, 1, 0, 1, 1, 1);

    //        var actual = _sut.GetData(expected);
    //        actual.Should().BeEquivalentTo(expected);
    //    }
    //    [Fact]
    //    public void ListDataTest()
    //    {
    //        using var trans = TransHelper.NewScope();
    //        var expected = new LayananDkModel("01", "Penyakit Dalam", 1, 1, 1, 0, 1, 1, 1);

    //        var actual = _sut.ListData();
    //        actual.Should().ContainEquivalentOf(expected);
    //    }
    //}

    
        

}

public record LayananDkDto(
        string fs_kd_layanan_dk,
        string fs_nm_layanan_dk,
        decimal fn_rawat_inap,
        decimal fn_rawat_jalan,
        decimal fn_kesehatan_jiwa,
        decimal fn_bedah,
        decimal fn_rujukan,
        decimal fn_kunj_rumah,
        decimal fn_layanan_sub
    )
{
    public LayananDkType ToModel()
    {
        return new LayananDkType(fs_kd_layanan_dk, fs_nm_layanan_dk,
            (int)fn_rawat_inap, (int)fn_rawat_jalan, (int)fn_kesehatan_jiwa, (int)fn_bedah,
            (int)fn_rujukan, (int)fn_kunj_rumah, (int)fn_layanan_sub);


    }
}
