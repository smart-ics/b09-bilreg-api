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

public interface IGroupTarifDal :
    IInsert<GroupTarifDto>,
    IUpdate<GroupTarifDto>,
    IDelete<IGroupTarifKey>,
    IGetData<GroupTarifDto, IGroupTarifKey>,
    IListData<GroupTarifDto>
{
}

public class GroupTarifDal : IGroupTarifDal
{
    private readonly DatabaseOptions _opt;

    public GroupTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GroupTarifDto dto)
    {
        const string sql = """
            INSERT INTO ta_grup_tarif(
                fs_kd_grup_tarif, fs_nm_grup_tarif)
            VALUES( 
                @fs_kd_grup_tarif, @fs_nm_grup_tarif)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", dto.fs_kd_grup_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_tarif", dto.fs_nm_grup_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GroupTarifDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_grup_tarif
           SET
               fs_nm_grup_tarif = @fs_nm_grup_tarif
           WHERE
               fs_kd_grup_tarif = @fs_kd_grup_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", dto.fs_kd_grup_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_tarif", dto.fs_nm_grup_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGroupTarifKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_grup_tarif
           WHERE
               fs_kd_grup_tarif = @fs_kd_grup_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", key.GroupTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GroupTarifDto GetData(IGroupTarifKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_grup_tarif,
               fs_nm_grup_tarif
           FROM 
               ta_grup_tarif
           WHERE
               fs_kd_grup_tarif = @fs_kd_grup_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif", key.GroupTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GroupTarifDto>(sql, dp);
        return result;
    }

    public IEnumerable<GroupTarifDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_grup_tarif,
                fs_nm_grup_tarif
            FROM 
                ta_grup_tarif
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GroupTarifDto>(sql);
    }
}

public class GroupTarifDalTest
{
    private readonly GroupTarifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static GroupTarifDto Faker()
        => new GroupTarifDto("A", "B");

    private static IGroupTarifKey FakerKey()
        => GroupTarifType.Default with { GroupTarifId = "A" };

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