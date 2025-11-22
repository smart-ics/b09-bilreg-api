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

public interface IKomponenSatTugasDal:
    IInsertBulk<KomponenSatTugasDto>,
    IDelete<IKomponenKey>,
    IListData<KomponenSatTugasDto, IKomponenKey>
{
}

public class KomponenSatTugasDal : IKomponenSatTugasDal
{
    private readonly DatabaseOptions _opt;

    public KomponenSatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<KomponenSatTugasDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("fs_kd_detil_tarif", "fs_kd_detil_tarif");
        bcp.AddMap("fs_kd_sat_tugas", "fs_kd_sat_tugas");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_detil_tarif2";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IKomponenKey key)
    {
        const string sql = """
            DELETE FROM
                ta_detil_tarif2
            WHERE
                fs_kd_detil_tarif = @fs_kd_detil_tarif
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", key.KomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    }

    public IEnumerable<KomponenSatTugasDto> ListData(IKomponenKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_detil_tarif, aa.fs_kd_sat_tugas, 
                ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas,
                ISNULL(bb.fs_kd_profesi, '') AS fs_kd_profesi,
                ISNULL(cc.ProfesiName, '') AS fs_nm_profesi
            FROM 
                ta_detil_tarif2 aa
                LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
                LEFT JOIN BILRG_Profesi cc ON bb.fs_kd_profesi = cc.ProfesiId
            WHERE
                aa.fs_kd_detil_tarif = @fs_kd_detil_tarif
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", filter.KomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KomponenSatTugasDto>(sql, dp);
    }
}

public class KomponenSatTugasDalTest
{
    private readonly KomponenSatTugasDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<KomponenSatTugasDto> FakerList()
        => new List<KomponenSatTugasDto>
        {
            new KomponenSatTugasDto(
                fs_kd_detil_tarif: "A",
                fs_kd_sat_tugas: "B",
                fs_nm_sat_tugas: "C",
                fs_kd_profesi: "D",
                fs_nm_profesi: "E"
            ),
            new KomponenSatTugasDto(
                fs_kd_detil_tarif: "A",
                fs_kd_sat_tugas: "F",
                fs_nm_sat_tugas: "G",
                fs_kd_profesi: "H",
                fs_nm_profesi: "I"
            )
        };

    private static IKomponenKey FakerKey()
        => KomponenType.Key("A");

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
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt.Excluding(x => x.fs_nm_sat_tugas)
                .Excluding(x => x.fs_kd_profesi)
                .Excluding(x => x.fs_nm_profesi));
    }
}
