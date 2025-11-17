using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BillContext.TindakanFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

public interface IKomponenDal :
    IInsert<KomponenDto>,
    IUpdate<KomponenDto>,
    IDelete<IKomponenKey>,
    IGetData<KomponenDto, IKomponenKey>,
    IListData<KomponenDto>
{
}

public class KomponenDal : IKomponenDal
{
    private readonly DatabaseOptions _opt;

    public KomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KomponenDto dto)
    {
        const string sql = """
           INSERT INTO ta_detil_tarif(
               fs_kd_detil_tarif, fs_nm_detil_tarif, fs_kd_grup_detil_tarif)
           VALUES( 
               @fs_kd_detil_tarif, @fs_nm_detil_tarif, @fs_kd_grup_detil_tarif)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", dto.fs_kd_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_detil_tarif", dto.fs_nm_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_detil_tarif", dto.fs_kd_grup_detil_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KomponenDto dto)
    {
        const string sql = """
           UPDATE 
               ta_detil_tarif
           SET
               fs_kd_detil_tarif = @fs_kd_detil_tarif, 
               fs_nm_detil_tarif = @fs_nm_detil_tarif, 
               fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif
           WHERE 
              fs_kd_detil_tarif = @fs_kd_detil_tarif
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", dto.fs_kd_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_detil_tarif", dto.fs_nm_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_detil_tarif", dto.fs_kd_grup_detil_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKomponenKey key)
    {
        const string sql = """
           DELETE FROM 
               ta_detil_tarif
           WHERE 
              fs_kd_detil_tarif = @fs_kd_detil_tarif
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", key.KomponenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KomponenDto GetData(IKomponenKey key)
    {
        const string sql = """
            SELECT
                aa.fs_kd_detil_tarif, aa.fs_nm_detil_tarif, aa.fs_kd_grup_detil_tarif,
                ISNULL(bb.fs_nm_grup_detil_tarif, '') fs_nm_grup_detil_tarif
            FROM
                ta_detil_tarif aa
                LEFT JOIN ta_grup_detil_tarif bb ON aa.fs_kd_grup_detil_tarif = bb.fs_kd_grup_detil_tarif
            WHERE
                aa.fs_kd_detil_tarif = @fs_kd_detil_tarif
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", key.KomponenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KomponenDto>(sql, dp);
        return result;
    }

    public IEnumerable<KomponenDto> ListData()
    {
        const string sql = """
           SELECT
               aa.fs_kd_detil_tarif, aa.fs_nm_detil_tarif, aa.fs_kd_grup_detil_tarif,
               ISNULL(bb.fs_nm_grup_detil_tarif, '') fs_nm_grup_detil_tarif
           FROM
               ta_detil_tarif aa
               LEFT JOIN ta_grup_detil_tarif bb ON aa.fs_kd_grup_detil_tarif = bb.fs_kd_grup_detil_tarif
           """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KomponenDto>(sql);
        return result;
    }
}

public class KomponenDalTest
{
    private readonly KomponenDal _sut = new(ConnStringHelper.GetTestEnv());

    private static KomponenDto Faker()
        => new KomponenDto("A", "B", "C", "D");

    private static IKomponenKey FakerKey()
        => KomponenType.Default with { KomponenId = "A" };

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
            opt => opt
                .Excluding(x => x.fs_nm_grup_detil_tarif));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.fs_nm_grup_detil_tarif));
    }
}