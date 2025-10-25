using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;

public interface IPetugasMedisSatTugasDal :
    IInsertBulk<PetugasMedisSatTugasDto>,
    IDelete<IPetugasMedisKey>,
    IListData<PetugasMedisSatTugasDto, IPetugasMedisKey>
{
}

public class PetugasMedisSatTugasDal : IPetugasMedisSatTugasDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisSatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PetugasMedisSatTugasDto> listModel)
    {
        //  INSERT BULK
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        bcp.AddMap("fs_kd_peg", "fs_kd_peg");
        bcp.AddMap("fs_kd_sat_tugas", "fs_kd_sat_tugas");
        bcp.AddMap("fn_utama", "fn_utama");

        conn.Open();
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "td_peg_sat_tugas";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPetugasMedisKey key)
    {
        const string sql = """
            DELETE FROM 
                td_peg_sat_tugas
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PetugasMedisSatTugasDto> ListData(IPetugasMedisKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_sat_tugas, aa.fn_utama,
                ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas
            FROM 
                td_peg_sat_tugas aa
                LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PetugasMedisSatTugasDto>(sql, dp);
    }
}

public class PetugasMedisSatTugasDalTest
{
    private readonly PetugasMedisSatTugasDal _sut = new(ConnStringHelper.GetTestEnv());
    
    private static PetugasMedisSatTugasDto Faker() => new PetugasMedisSatTugasDto(
        "A", "B", 1, "C");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new List<PetugasMedisSatTugasDto> { Faker() });
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(PetugasMedisType.Key("A"));
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new List<PetugasMedisSatTugasDto> { Faker() });
        var actual = _sut.ListData(PetugasMedisType.Key("A"));
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt.Excluding(x => x.fs_nm_sat_tugas));
    }
}