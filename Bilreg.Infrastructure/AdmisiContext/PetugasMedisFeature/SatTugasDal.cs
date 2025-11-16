using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

public interface ISatTugasDal :
    IInsert<SatTugasDto>,
    IUpdate<SatTugasDto>,
    IDelete<ISatTugasKey>,
    IGetData<SatTugasDto, ISatTugasKey>,
    IListData<SatTugasDto>
{
}

public class SatTugasDal : ISatTugasDal
{
    private readonly DatabaseOptions _opt;

    public SatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(SatTugasDto dto)
    {
        const string sql = """
            INSERT INTO td_sat_tugas(
                fs_kd_sat_tugas, fs_nm_sat_tugas, fb_sat_medis)
            VALUES( 
                @fs_kd_sat_tugas, @fs_nm_sat_tugas, @fb_sat_medis)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", dto.fs_kd_sat_tugas, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_sat_tugas", dto.fs_nm_sat_tugas, SqlDbType.VarChar);
        dp.AddParam("@fb_sat_medis", dto.fb_sat_medis, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SatTugasDto dto)
    {
        const string sql = @"
           UPDATE 
               td_sat_tugas
           SET
               fs_nm_sat_tugas = @fs_nm_sat_tugas,
               fb_sat_medis = @fb_sat_medis
           WHERE
               fs_kd_sat_tugas = @fs_kd_sat_tugas";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", dto.fs_kd_sat_tugas, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_sat_tugas", dto.fs_nm_sat_tugas, SqlDbType.VarChar);
        dp.AddParam("@fb_sat_medis", dto.fb_sat_medis, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISatTugasKey key)
    {
        const string sql = @"
           DELETE FROM 
                td_sat_tugas
           WHERE
               fs_kd_sat_tugas = @fs_kd_sat_tugas";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", key.SatTugasId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public SatTugasDto GetData(ISatTugasKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_sat_tugas,
               fs_nm_sat_tugas,
               fb_sat_medis
           FROM 
               td_sat_tugas
           WHERE
               fs_kd_sat_tugas = @fs_kd_sat_tugas";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", key.SatTugasId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<SatTugasDto>(sql, dp);
        return result;
    }

    public IEnumerable<SatTugasDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_sat_tugas,
                fs_nm_sat_tugas,
                fb_sat_medis
            FROM 
                td_sat_tugas
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<SatTugasDto>(sql);
    }
}

public class SatTugasDalTest
{
    private readonly SatTugasDal _sut = new(ConnStringHelper.GetTestEnv());

    private static SatTugasDto Faker()
        => new SatTugasDto("A", "B", true);

    private static ISatTugasKey FakerKey()
        => SatTugasType.Default with { SatTugasId = "A" };

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