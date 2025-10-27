using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RegSub;

public interface IRegAktifDal :
    IInsert<RegAktifDto>,
    IUpdate<RegAktifDto>,
    IDelete<IRegKey>,
    IGetData<RegAktifDto, IRegKey>,
    IListData<RegAktifDto, Periode>
{
}

public class RegAktifDal : IRegAktifDal
{
    private readonly DatabaseOptions _opt;

    public RegAktifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RegAktifDto model)
    {
        const string sql = """
            INSERT INTO BILRG_RegAktif(
                RegId, RegDate, PasienId, JenisRawat, 
                LayananId, DokterId, TipeJaminanId)
            VALUES(
                @RegId, @RegDate, @PasienId, @JenisRawat, 
                @LayananId, @DokterId, @TipeJaminanId)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", model.RegId, SqlDbType.VarChar); 
        dp.AddParam("@RegDate", model.RegDate, SqlDbType.VarChar);	 
        dp.AddParam("@PasienId", model.PasienId, SqlDbType.VarChar);	 
        dp.AddParam("@JenisRawat", model.JenisRawat, SqlDbType.VarChar);	 
        dp.AddParam("@LayananId", model.LayananId, SqlDbType.VarChar);	 
        dp.AddParam("@DokterId", model.DokterId, SqlDbType.VarChar);	 
        dp.AddParam("@TipeJaminanId", model.TipeJaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RegAktifDto model)
    {
        const string sql = """
           UPDATE 
                BILRG_RegAktif
           SET
              RegDate = @RegDate, 
              PasienId = @PasienId, 
              JenisRawat = @JenisRawat, 
              LayananId = @LayananId, 
              DokterId = @DokterId, 
              TipeJaminanId = @TipeJaminanId
           WHERE
              RegId = @RegId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", model.RegId, SqlDbType.VarChar); 
        dp.AddParam("@RegDate", model.RegDate, SqlDbType.VarChar);	 
        dp.AddParam("@PasienId", model.PasienId, SqlDbType.VarChar);	 
        dp.AddParam("@JenisRawat", model.JenisRawat, SqlDbType.VarChar);	 
        dp.AddParam("@LayananId", model.LayananId, SqlDbType.VarChar);	 
        dp.AddParam("@DokterId", model.DokterId, SqlDbType.VarChar);	 
        dp.AddParam("@TipeJaminanId", model.TipeJaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRegKey key)
    {
        const string sql = """
           DELETE FROM
               BILRG_RegAktif
           WHERE
             RegId = @RegId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RegAktifDto GetData(IRegKey key)
    {
        const string sql = """
           SELECT
               aa.RegId, aa.RegDate, aa.PasienId, aa.JenisRawat, 
               aa.LayananId, aa.DokterId, aa.TipeJaminanId,
               ISNULL(bb.fs_nm_pasien, '') AS PasienName,
               ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '-') AS Gender,
               ISNULL(cc.fs_nm_layanan, '') AS LayananName,
               ISNULL(dd.fs_nm_peg, '') AS DokterName,
               ISNULL(ee.fs_nm_tipe_jaminan, '') AS TipeJaminanName
           FROM
               BILRG_RegAktif aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
               LEFT JOIN td_peg dd ON aa.DokterId = dd.fs_kd_peg
               LEFT JOIN ta_tipe_jaminan ee ON aa.TipeJaminanId = ee.fs_kd_tipe_jaminan
           WHERE
               aa.RegId = @RegId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar); 


        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RegAktifDto>(sql, dp);
    }

    public IEnumerable<RegAktifDto> ListData(Periode filter)
    {
        const string sql = """
           SELECT
               aa.RegId, aa.RegDate, aa.PasienId, aa.JenisRawat, 
               aa.LayananId, aa.DokterId, aa.TipeJaminanId,
               ISNULL(bb.fs_nm_pasien, '') AS PasienName,
               ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '-') AS Gender,
               ISNULL(cc.fs_nm_layanan, '') AS LayananName,
               ISNULL(dd.fs_nm_peg, '') AS DokterName,
               ISNULL(ee.fs_nm_tipe_jaminan, '') AS TipeJaminanName
           FROM
               BILRG_RegAktif aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
               LEFT JOIN td_peg dd ON aa.DokterId = dd.fs_kd_peg
               LEFT JOIN ta_tipe_jaminan ee ON aa.TipeJaminanId = ee.fs_kd_tipe_jaminan
           WHERE
               aa.RegDate BETWEEN @Tgl1 AND @Tgl2
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime); 
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegAktifDto>(sql, dp);
    }
}

public class RegAktifDalTest
{
    private readonly RegAktifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegAktifDto Faker()
        => new RegAktifDto(
            RegId: "A",
            RegDate: new DateTime(2024, 1, 2),
            PasienId: "C",
            JenisRawat: "D",
            LayananId: "E",
            DokterId: "F",
            TipeJaminanId: "G",
            PasienName: "H",
            TglLahir: "2000-01-03",
            Gender: "I",
            LayananName: "J",
            TipeJaminanName: "K",
            DokterName: "L"
        );

    private static IRegKey FakerKey()
        => RegModel.Key("A");

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker(), 
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.LayananName)
                      .Excluding(x => x.TipeJaminanName)
                      .Excluding(x => x.DokterName));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var regAktif = Faker();
        _sut.Insert(regAktif);
        
        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(regAktif,
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.LayananName)
                      .Excluding(x => x.TipeJaminanName)
                      .Excluding(x => x.DokterName));
    }
}
