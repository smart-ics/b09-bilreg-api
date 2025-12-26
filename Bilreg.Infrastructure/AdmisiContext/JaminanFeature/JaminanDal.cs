using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public interface IJaminanDal :
    //IInsert<JaminanDto>,
    //IUpdate<JaminanDto>,
    //IDelete<IJaminanKey>,
    IGetData<JaminanDto, IJaminanKey>,
    IListData<JaminanDto>
{
}
public class JaminanDal : IJaminanDal
{
    private readonly DatabaseOptions _opt;

    public JaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    //public void Insert(JaminanDto model)
    //{
    //    const string sql = @"
    //         INSERT INTO ta_jaminan (
    //             fs_kd_jaminan, fs_nm_jaminan, fs_alm1_jaminan, fs_alm2_jaminan,
    //             fs_kota_jaminan, fb_aktif, fs_kd_cara_bayar_dk, fs_kd_grup_jaminan)
    //         VALUES (
    //             @fs_kd_jaminan, @fs_nm_jaminan, @fs_alm1_jaminan, @fs_alm2_jaminan,
    //             @fs_kota_jaminan, @fb_aktif, @fs_kd_cara_bayar_dk, @fs_kd_grup_jaminan )";

    //    var listAlamat = model.Alamat.Normalize3Address();
        
    //    var dp = new DynamicParameters();
    //    dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);
    //    dp.AddParam("@fs_nm_jaminan", model.JaminanName, SqlDbType.VarChar);
    //    dp.AddParam("@fs_alm1_jaminan", listAlamat[0], SqlDbType.VarChar);
    //    dp.AddParam("@fs_alm2_jaminan", listAlamat[1], SqlDbType.VarChar);
    //    dp.AddParam("@fs_kota_jaminan", model.Alamat.Kota, SqlDbType.VarChar);

    //    dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
    //    dp.AddParam("@fs_kd_cara_bayar_dk", model.CaraBayarDk.CaraBayarDkId, SqlDbType.VarChar);
    //    dp.AddParam("@fs_kd_grup_jaminan", model.GroupJaminan.GroupJaminanId, SqlDbType.VarChar);

    //    using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //    conn.Execute(sql, dp);
    //}

    //public void Update(JaminanDto model)
    //{
    //    const string sql = @"
    //         UPDATE 
    //             ta_jaminan
    //         SET 
    //             fs_nm_jaminan = @fs_nm_jaminan,
    //             fs_alm1_jaminan = @fs_alm1_jaminan,
    //             fs_alm2_jaminan = @fs_alm2_jaminan,
    //             fs_kota_jaminan = @fs_kota_jaminan,
    //             fb_aktif = @fb_aktif,
    //             fs_kd_cara_bayar_dk = @fs_kd_cara_bayar_dk,
    //             fs_kd_grup_jaminan = @fs_kd_grup_jaminan
    //         WHERE 
    //             fs_kd_jaminan = @fs_kd_jaminan";
        
    //    var listAlamat = model.Alamat.Normalize3Address();
        
    //    var dp = new DynamicParameters();
    //    dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);
    //    dp.AddParam("@fs_nm_jaminan", model.JaminanName, SqlDbType.VarChar);
    //    dp.AddParam("@fs_alm1_jaminan", listAlamat[0], SqlDbType.VarChar);
    //    dp.AddParam("@fs_alm2_jaminan", listAlamat[1], SqlDbType.VarChar);
    //    dp.AddParam("@fs_kota_jaminan", model.Alamat.Kota, SqlDbType.VarChar);

    //    dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
    //    dp.AddParam("@fs_kd_cara_bayar_dk", model.CaraBayarDk.CaraBayarDkId, SqlDbType.VarChar);
    //    dp.AddParam("@fs_kd_grup_jaminan", model.GroupJaminan.GroupJaminanId, SqlDbType.VarChar);

    //    using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //    conn.Execute(sql, dp);
    //}

    //public void Delete(IJaminanKey key)
    //{
    //    const string sql = @"
    //         DELETE FROM ta_jaminan
    //         WHERE fs_kd_jaminan = @fs_kd_jaminan ";

    //    var dp = new DynamicParameters();
    //    dp.AddParam("@fs_kd_jaminan", key.JaminanId, SqlDbType.VarChar);

    //    using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //    conn.Execute(sql, dp);
    //}

    public JaminanDto GetData(IJaminanKey key)
    {
        const string sql = """
           SELECT
               aa.fs_kd_jaminan, aa.fs_nm_jaminan, 
               aa.fs_alm1_jaminan, aa.fs_alm2_jaminan, aa.fs_kota_jaminan, 
               aa.fb_aktif, aa.fs_benefit_mou, aa.fs_kd_cara_bayar_dk, 
               aa.fs_kd_grup_jaminan, 
               aa.fs_kd_tipe_tarif_rawat_jalan, aa.fs_kd_tipe_tarif_rawat_inap,
           	   '-' as fs_kd_pos,
               ISNULL(bb.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk,
               ISNULL(cc.fs_nm_grup_jaminan, '-') fs_nm_grup_jaminan,
               ISNULL(dd.fs_nm_tarif_tipe,'') AS fs_nm_tarif_tipe_rawat_jalan,
               ISNULL(ee.fs_nm_tarif_tipe,'') AS fs_nm_tarif_tipe_rawat_inap

           FROM 
               ta_jaminan aa
               LEFT JOIN ta_cara_bayar_dk bb ON aa.fs_kd_cara_bayar_dk = bb.fs_kd_cara_bayar_dk
               LEFT JOIN ta_grup_jaminan cc ON aa.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
           	   LEFT JOIN TA_TARIF_TIPE dd ON aa.FS_KD_TIPE_TARIF_RAWAT_JALAN = dd.FS_KD_TARIF_TIPE
           	   LEFT JOIN TA_TARIF_TIPE ee ON aa.FS_KD_TIPE_TARIF_RAWAT_INAP = ee.FS_KD_TARIF_TIPE
           WHERE 
               aa.fs_kd_jaminan = @fs_kd_jaminan
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", key.JaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JaminanDto>(sql, dp);
    }

    public IEnumerable<JaminanDto> ListData()
    {
        const string sql = """
           SELECT
               aa.fs_kd_jaminan, aa.fs_nm_jaminan, 
               aa.fs_alm1_jaminan, aa.fs_alm2_jaminan, aa.fs_kota_jaminan, 
               aa.fb_aktif, aa.fs_benefit_mou, aa.fs_kd_cara_bayar_dk, 
               aa.fs_kd_grup_jaminan, 
               aa.fs_kd_tipe_tarif_rawat_jalan, aa.fs_kd_tipe_tarif_rawat_inap,
           	   '-' as fs_kd_pos,
               ISNULL(bb.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk,
               ISNULL(cc.fs_nm_grup_jaminan, '-') fs_nm_grup_jaminan,
               ISNULL(dd.fs_nm_tarif_tipe,'') AS fs_nm_tarif_tipe_rawat_jalan,
               ISNULL(ee.fs_nm_tarif_tipe,'') AS fs_nm_tarif_tipe_rawat_inap

           FROM 
               ta_jaminan aa
               LEFT JOIN ta_cara_bayar_dk bb ON aa.fs_kd_cara_bayar_dk = bb.fs_kd_cara_bayar_dk
               LEFT JOIN ta_grup_jaminan cc ON aa.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
           	   LEFT JOIN TA_TARIF_TIPE dd ON aa.FS_KD_TIPE_TARIF_RAWAT_JALAN = dd.FS_KD_TARIF_TIPE
           	   LEFT JOIN TA_TARIF_TIPE ee ON aa.FS_KD_TIPE_TARIF_RAWAT_INAP = ee.FS_KD_TARIF_TIPE
           WHERE 
               aa.fs_kd_jaminan = @fs_kd_jaminan
           """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas =  conn.Read<JaminanDto>(sql);
        return datas;   
    }

    

}

public class JaminanDalTest
{
    private readonly JaminanDal _sut;

    public JaminanDalTest()
    {
        _sut = new JaminanDal(ConnStringHelper.GetTestEnv());
    }

    //[Fact]
    //public void InsertTest()
    //{
    //    var alamat = new AlamatType([], "C", "D");
    //    using var trans = TransHelper.NewScope();
    //    var expected = new JaminanType("A", "B", false, alamat, 
    //        CaraBayarDkType.Default, GroupJaminanType.Default.ToReff());
    //    _sut.Insert(expected);
    //}

    //[Fact]
    //public void UpdateTest()
    //{
    //    using var trans = TransHelper.NewScope();
    //    var expected = new JaminanType("A", "B", false, AlamatType.Default,
    //        CaraBayarDkType.Default, GroupJaminanType.Default.ToReff());
    //    _sut.Update(expected);
    //}

    //[Fact]
    //public void DeleteTest()
    //{
    //    using var trans = TransHelper.NewScope();
    //    var key =  new JaminanType("A", "B", false, AlamatType.Default,
    //        CaraBayarDkType.Default, GroupJaminanType.Default.ToReff());
    //    _sut.Delete(key);
    //}

    //[Fact]
    //public void GetDataTest()
    //{
    //    using var trans = TransHelper.NewScope();
    //    var expected = new JaminanType("A", "B", false, AlamatType.Default,
    //        CaraBayarDkType.Default, GroupJaminanType.Default.ToReff());
    //    _sut.Insert(expected);
    //    var actual = _sut.GetData(expected).Value;
    //    actual.Should().BeEquivalentTo(expected);
    //}

    //[Fact]
    //public void ListDataTest()
    //{
    //    using var trans = TransHelper.NewScope();
    //    var expected = new JaminanType("A", "B", true, AlamatType.Default,
    //        CaraBayarDkType.Default, GroupJaminanType.Default.ToReff());
    //    _sut.Insert(expected);
    //    var actual = _sut.ListData().Value;
    //    actual.Should().ContainEquivalentOf(expected);
    //}
}