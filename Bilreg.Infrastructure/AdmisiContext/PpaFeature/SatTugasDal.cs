using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

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
                               fs_kd_sat_tugas, fs_nm_sat_tugas, fs_kd_profesi)
                           VALUES( 
                               @fs_kd_sat_tugas, @fs_nm_sat_tugas, @fs_kd_profesi)
                           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", dto.fs_kd_sat_tugas, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_sat_tugas", dto.fs_nm_sat_tugas, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_profesi", dto.fs_kd_profesi, SqlDbType.VarChar);

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
               fs_kd_profesi = @fs_kd_profesi
           WHERE
               fs_kd_sat_tugas = @fs_kd_sat_tugas";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", dto.fs_kd_sat_tugas, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_sat_tugas", dto.fs_nm_sat_tugas, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_profesi", dto.fs_kd_profesi, SqlDbType.VarChar);

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
        const string sql = """
                           SELECT
                               aa.fs_kd_sat_tugas, aa.fs_nm_sat_tugas, aa.fs_kd_profesi,
                               ISNULL(bb.fs_nm_profesi, '') AS fs_nm_profesi
                           FROM 
                               td_sat_tugas aa
                               LEFT JOIN BILRG_Profesi bb ON aa.fs_kd_profesi = bb.ProfesiId
                           WHERE
                               fs_kd_sat_tugas = @fs_kd_sat_tugas
                           """;
        
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
                               aa.fs_kd_sat_tugas, aa.fs_nm_sat_tugas, aa.fs_kd_profesi,
                               ISNULL(bb.fs_nm_profesi, '') AS fs_nm_profesi
                           FROM 
                               td_sat_tugas aa
                               LEFT JOIN BILRG_Profesi bb ON aa.fs_kd_profesi = bb.ProfesiId
                           """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<SatTugasDto>(sql);
    }
}

public class SatTugasDalTest
{
    private readonly SatTugasDal _sut = new(ConnStringHelper.GetTestEnv());

    private static SatTugasDto Faker()
        => new SatTugasDto("A", "B", "C", "D");

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
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt.Excluding(x => x.fs_nm_profesi));;
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt.Excluding(x => x.fs_nm_profesi));
    }
}