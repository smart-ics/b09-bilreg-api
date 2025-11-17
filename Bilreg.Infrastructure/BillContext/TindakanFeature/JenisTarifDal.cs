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

public interface IJenisTarifDal :
    IInsert<JenisTarifDto>,
    IUpdate<JenisTarifDto>,
    IDelete<IJenisTarifKey>,
    IGetData<JenisTarifDto, IJenisTarifKey>,
    IListData<JenisTarifDto>
{
}

public class JenisTarifDal : IJenisTarifDal
{
    private readonly DatabaseOptions _opt;

    public JenisTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(JenisTarifDto dto)
    {
        const string sql = """
            INSERT INTO ta_jenis_tarif(
                fs_kd_jenis_tarif, fs_nm_jenis_tarif, fs_urut)
            VALUES( 
                @fs_kd_jenis_tarif, @fs_nm_jenis_tarif, @fs_urut)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", dto.fs_kd_jenis_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jenis_tarif", dto.fs_nm_jenis_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_urut", dto.fs_urut, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JenisTarifDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_jenis_tarif
           SET
               fs_nm_jenis_tarif = @fs_nm_jenis_tarif,
               fs_urut = @fs_urut
           WHERE
               fs_kd_jenis_tarif = @fs_kd_jenis_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", dto.fs_kd_jenis_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jenis_tarif", dto.fs_nm_jenis_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_urut", dto.fs_urut, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJenisTarifKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_jenis_tarif
           WHERE
               fs_kd_jenis_tarif = @fs_kd_jenis_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", key.JenisTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public JenisTarifDto GetData(IJenisTarifKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_jenis_tarif,
               fs_nm_jenis_tarif,
               fs_urut
           FROM 
               ta_jenis_tarif
           WHERE
               fs_kd_jenis_tarif = @fs_kd_jenis_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", key.JenisTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<JenisTarifDto>(sql, dp);
        return result;
    }

    public IEnumerable<JenisTarifDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_jenis_tarif,
                fs_nm_jenis_tarif,
                fs_urut
            FROM 
                ta_jenis_tarif
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JenisTarifDto>(sql);
    }
}

public class JenisTarifDalTest
{
    private readonly JenisTarifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static JenisTarifDto Faker()
        => new JenisTarifDto("A", "B", "1");

    private static IJenisTarifKey FakerKey()
        => JenisTarifType.Default with { JenisTarifId = "A" };

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
        actual.Should().BeEquivalentTo(Faker());
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker());
    }
}