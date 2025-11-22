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

public interface IProfesiDal :
    IInsert<ProfesiType>,
    IUpdate<ProfesiType>,
    IDelete<IProfesiKey>,
    IGetData<ProfesiType, IProfesiKey>,
    IListData<ProfesiType>
{
}

public class ProfesiDal : IProfesiDal
{
    private readonly DatabaseOptions _opt;

    public ProfesiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(ProfesiType dto)
    {
        const string sql = """
            INSERT INTO BILRG_Profesi(
                ProfesiId, ProfesiName)
            VALUES( 
                @ProfesiId, @ProfesiName)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ProfesiId", dto.ProfesiId, SqlDbType.VarChar);
        dp.AddParam("@ProfesiName", dto.ProfesiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(ProfesiType dto)
    {
        const string sql = @"
           UPDATE 
               BILRG_Profesi
           SET
               ProfesiName = @ProfesiName
           WHERE
               ProfesiId = @ProfesiId";

        var dp = new DynamicParameters();
        dp.AddParam("@ProfesiId", dto.ProfesiId, SqlDbType.VarChar);
        dp.AddParam("@ProfesiName", dto.ProfesiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IProfesiKey key)
    {
        const string sql = @"
           DELETE FROM 
                BILRG_Profesi
           WHERE
               ProfesiId = @ProfesiId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@ProfesiId", key.ProfesiId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public ProfesiType GetData(IProfesiKey key)
    {
        const string sql = @"
           SELECT
               ProfesiId,
               ProfesiName
           FROM 
               BILRG_Profesi
           WHERE
               ProfesiId = @ProfesiId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@ProfesiId", key.ProfesiId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<ProfesiType>(sql, dp);
        return result;
    }

    public IEnumerable<ProfesiType> ListData()
    {
        const string sql = """
            SELECT
                ProfesiId,
                ProfesiName
            FROM 
                BILRG_Profesi
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ProfesiType>(sql);
    }
}

public class ProfesiDalTest
{
    private readonly ProfesiDal _sut = new(ConnStringHelper.GetTestEnv());

    private static ProfesiType Faker()
        => ProfesiType.Create("A", "B");

    private static IProfesiKey FakerKey()
        => ProfesiType.Default with { ProfesiId = "A" };

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