using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public class PetugasMedisSatTugasDal : IPetugasMedisSatTugasDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisSatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PetugasMedisSatTugasModel> listModel)
    {
        //  INSERT BULK
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        bcp.AddMap("PetugasMedisId", "fs_kd_peg");
        bcp.AddMap("SatTugasId", "fs_kd_sat_tugas");
        bcp.AddMap("IsUtama", "fn_utama");

        conn.Open();
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "td_peg_sat_tugas";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPetugasMedisKey key)
    {
        const string sql = @"
            DELETE FROM 
                td_peg_sat_tugas
            WHERE 
                fs_kd_peg = @fs_kd_peg";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PetugasMedisSatTugasModel> ListData(IPetugasMedisKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_sat_tugas, aa.fn_utama,
                ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas
            FROM 
                td_peg_sat_tugas aa
                LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PetugasMedisSatTugasDto>(sql, dp);
    }
}

public class PetugasMedisSatTugasDto() : PetugasMedisSatTugasModel(string.Empty, string.Empty, string.Empty)
{
    public string fs_kd_peg { get => PetugasMedisId; set => PetugasMedisId = value; }
    public string fs_kd_sat_tugas { get => SatTugasId; set => SatTugasId = value; }
    public string fs_nm_sat_tugas { get => SatTugasName; set => SatTugasName = value; }
    public int fn_utama { get => Convert.ToInt16(IsUtama); set => IsUtama = Convert.ToBoolean(value); }
}

public class PetugasMedisSatTugasDalTest
{
    private readonly PetugasMedisSatTugasDal _sut;

    public PetugasMedisSatTugasDalTest()
    {
        _sut = new PetugasMedisSatTugasDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PetugasMedisSatTugasModel("A", "B", "C");
        expected.SetUtama();
        _sut.Insert(new List<PetugasMedisSatTugasModel> { expected });
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PetugasMedisSatTugasModel("A", "B", "C");
        expected.SetUtama();
        _sut.Delete(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PetugasMedisSatTugasModel("A", "B", "");
        expected.SetUtama();
        _sut.Insert(new List<PetugasMedisSatTugasModel> { expected });
        var actual = _sut.ListData(expected);
        actual.Should().BeEquivalentTo(new List<PetugasMedisSatTugasModel> { expected });
    }
}