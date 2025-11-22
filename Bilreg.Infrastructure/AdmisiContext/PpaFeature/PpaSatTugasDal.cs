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

public interface IPetugasMedisSatTugasDal :
    IInsertBulk<PpaSatTugasDto>,
    IDelete<IPpaKey>,
    IListData<PpaSatTugasDto, IPpaKey>,
    IListData<PpaSatTugasDto, ISatTugasKey>
{
}

public class PpaSatTugasDal : IPetugasMedisSatTugasDal
{
    private readonly DatabaseOptions _opt;

    public PpaSatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PpaSatTugasDto> listModel)
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

    public void Delete(IPpaKey key)
    {
        const string sql = """
            DELETE FROM 
                td_peg_sat_tugas
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PpaId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PpaSatTugasDto> ListData(IPpaKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_sat_tugas, aa.fn_utama, 
                ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas,
                ISNULL(bb.fs_kd_profesi, '') AS fs_kd_profesi,
                ISNULL(cc.ProfesiName, '') AS fs_nm_profesi
            FROM 
                td_peg_sat_tugas aa
                LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
                LEFT JOIN BILRG_Profesi cc ON bb.fs_kd_profesi = cc.ProfesiId    
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PpaId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PpaSatTugasDto>(sql, dp);
    }

    public IEnumerable<PpaSatTugasDto> ListData(ISatTugasKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_sat_tugas, aa.fn_utama, 
                ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas,
                ISNULL(bb.fs_kd_profesi, '') AS fs_kd_profesi,
                ISNULL(cc.ProfesiName, '') AS fs_nm_profesi
            FROM 
                td_peg_sat_tugas aa
                LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
                LEFT JOIN BILRG_Profesi cc ON bb.fs_kd_profesi = cc.ProfesiId    
            WHERE 
                aa.fs_kd_sat_tugas = @fs_kd_sat_tugas
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", filter.SatTugasId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PpaSatTugasDto>(sql, dp);
    }
}

public class PpaSatTugasDalTest
{
    private readonly PpaSatTugasDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<PpaSatTugasDto> FakerList()
        => new List<PpaSatTugasDto>
        {
            new PpaSatTugasDto(
                fs_kd_peg: "A",
                fs_kd_sat_tugas: "B",
                fn_utama: 1,
                fs_kd_profesi: "C",
                fs_nm_sat_tugas: "D",
                fs_nm_profesi: "E"
            )
        };

    private static IPpaKey FakerKey()
        => PpaType.Key("A");

    private static ISatTugasKey FakerSatTugasKey()
        => SatTugasType.Key("B");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataByPetugasMedisTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt
                .Excluding(x => x.fs_nm_sat_tugas)
                .Excluding(x => x.fs_kd_profesi)
                .Excluding(x => x.fs_nm_profesi));
    }

    [Fact]
    public void ListDataBySatTugasTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerSatTugasKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt
                .Excluding(x => x.fs_nm_sat_tugas)
                .Excluding(x => x.fs_kd_profesi)
                .Excluding(x => x.fs_nm_profesi));
    }
}