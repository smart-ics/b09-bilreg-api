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

public interface IGroupTarifDkDal :
    IInsert<GroupTarifDkDto>,
    IUpdate<GroupTarifDkDto>,
    IDelete<IGroupTarifDkKey>,
    IGetData<GroupTarifDkDto, IGroupTarifDkKey>,
    IListData<GroupTarifDkDto>
{
}

public class GroupTarifDkDal : IGroupTarifDkDal
{
    private readonly DatabaseOptions _opt;

    public GroupTarifDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GroupTarifDkDto dto)
    {
        const string sql = """
                           INSERT INTO ta_grup_tarif_dk(
                               fs_kd_grup_tarif_dk, fs_nm_grup_tarif_dk)
                           VALUES( 
                               @fs_kd_grup_tarif_dk, @fs_nm_grup_tarif_dk)
                           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif_dk", dto.fs_kd_grup_tarif_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_tarif_dk", dto.fs_nm_grup_tarif_dk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GroupTarifDkDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_grup_tarif_dk
           SET
               fs_nm_grup_tarif_dk = @fs_nm_grup_tarif_dk
           WHERE
               fs_kd_grup_tarif_dk = @fs_kd_grup_tarif_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif_dk", dto.fs_kd_grup_tarif_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_tarif_dk", dto.fs_nm_grup_tarif_dk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGroupTarifDkKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_grup_tarif_dk
           WHERE
               fs_kd_grup_tarif_dk = @fs_kd_grup_tarif_dk";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif_dk", key.GroupTarifDkId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GroupTarifDkDto GetData(IGroupTarifDkKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_grup_tarif_dk,
               fs_nm_grup_tarif_dk
           FROM 
               ta_grup_tarif_dk
           WHERE
               fs_kd_grup_tarif_dk = @fs_kd_grup_tarif_dk";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif_dk", key.GroupTarifDkId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GroupTarifDkDto>(sql, dp);
        return result;
    }

    public IEnumerable<GroupTarifDkDto> ListData()
    {
        const string sql = """
                           SELECT
                               fs_kd_grup_tarif_dk,
                               fs_nm_grup_tarif_dk
                           FROM 
                               ta_grup_tarif_dk
                           """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GroupTarifDkDto>(sql);
    }
}

public class GroupTarifDkDalTest
{
    private readonly GroupTarifDkDal _sut = new(ConnStringHelper.GetTestEnv());

    private static GroupTarifDkDto Faker()
        => new GroupTarifDkDto("A", "B");

    private static IGroupTarifDkKey FakerKey()
        => GroupTarifDkType.Default with { GroupTarifDkId = "A" };

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