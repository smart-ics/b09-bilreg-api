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

public interface ISmfDal :
    IInsert<SmfDto>,
    IUpdate<SmfDto>,
    IDelete<ISmfKey>,
    IGetData<SmfDto, ISmfKey>,
    IListData<SmfDto>
{
}

public class SmfDal : ISmfDal
{
    private readonly DatabaseOptions _opt;

    public SmfDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(SmfDto dto)
    {
        const string sql = """
            INSERT INTO ta_smf(
                fs_kd_smf, fs_nm_smf)
            VALUES( 
                @fs_kd_smf, @fs_nm_smf)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_smf", dto.fs_kd_smf, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_smf", dto.fs_nm_smf, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SmfDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_smf
           SET
               fs_nm_smf = @fs_nm_smf
           WHERE
               fs_kd_smf = @fs_kd_smf";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_smf", dto.fs_kd_smf, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_smf", dto.fs_nm_smf, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISmfKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_smf
           WHERE
               fs_kd_smf = @fs_kd_smf";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_smf", key.SmfId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public SmfDto GetData(ISmfKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_smf,
               fs_nm_smf
           FROM 
               ta_smf
           WHERE
               fs_kd_smf = @fs_kd_smf";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_smf", key.SmfId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<SmfDto>(sql, dp);
        return result;
    }

    public IEnumerable<SmfDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_smf,
                fs_nm_smf
            FROM 
                ta_smf
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<SmfDto>(sql);
    }
}

public class SmfDalTest
{
    private readonly SmfDal _sut = new(ConnStringHelper.GetTestEnv());

    private static SmfDto Faker()
        => new SmfDto("A", "B");

    private static ISmfKey FakerKey()
        => SmfType.Default with { SmfId = "A" };

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