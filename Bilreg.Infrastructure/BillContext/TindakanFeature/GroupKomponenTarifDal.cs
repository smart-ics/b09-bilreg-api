using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BillContext.TindakanFeature;
using Bilreg.Infrastructure.BillContext.TindakanFeature.TindakanAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

public interface IGroupKomponenDal :
    IInsert<GroupKomponenDto>,
    IUpdate<GroupKomponenDto>,
    IDelete<IGroupKomponenKey>,
    IGetData<GroupKomponenDto, IGroupKomponenKey>,
    IListData<GroupKomponenDto>
{
}

public class GroupKomponenDal : IGroupKomponenDal
{
    private readonly DatabaseOptions _opt;

    public GroupKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(GroupKomponenDto dto)
    {
        const string sql = """
            INSERT INTO ta_grup_detil_tarif(
                fs_kd_grup_detil_tarif, fs_nm_grup_detil_tarif)
            VALUES( 
                @fs_kd_grup_detil_tarif, @fs_nm_grup_detil_tarif)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_detil_tarif", dto.fs_kd_grup_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_detil_tarif", dto.fs_nm_grup_detil_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GroupKomponenDto dto)
    {
        const string sql = @"
           UPDATE 
               ta_grup_detil_tarif
           SET
               fs_nm_grup_detil_tarif = @fs_nm_grup_detil_tarif
           WHERE
               fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_detil_tarif", dto.fs_kd_grup_detil_tarif, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_detil_tarif", dto.fs_nm_grup_detil_tarif, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGroupKomponenKey key)
    {
        const string sql = @"
           DELETE FROM 
                ta_grup_detil_tarif
           WHERE
               fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_detil_tarif", key.GroupKomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GroupKomponenDto GetData(IGroupKomponenKey key)
    {
        const string sql = @"
           SELECT
               fs_kd_grup_detil_tarif,
               fs_nm_grup_detil_tarif
           FROM 
               ta_grup_detil_tarif
           WHERE
               fs_kd_grup_detil_tarif = @fs_kd_grup_detil_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_detil_tarif", key.GroupKomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GroupKomponenDto>(sql, dp);
        return result;
    }

    public IEnumerable<GroupKomponenDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_grup_detil_tarif,
                fs_nm_grup_detil_tarif
            FROM 
                ta_grup_detil_tarif
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GroupKomponenDto>(sql);
    }
}

public class GroupKomponenDalTest
{
    private readonly GroupKomponenDal _sut = new(ConnStringHelper.GetTestEnv());

    private static GroupKomponenDto Faker()
        => new GroupKomponenDto("A", "B");

    private static IGroupKomponenKey FakerKey()
        => GroupKomponenType.Default with { GroupKomponenId = "A" };

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