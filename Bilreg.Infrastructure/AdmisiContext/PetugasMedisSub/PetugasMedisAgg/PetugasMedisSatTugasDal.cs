using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public class PetugasMedisSatTugasDal : IPetugasMedisSatTugasDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisSatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PetugasMedisSatTugasType> listModel)
    {
        throw new NotImplementedException();
    }

    public void Delete(IPetugasMedisKey key)
    {
        throw new NotImplementedException();
    }

    public MayBe<IEnumerable<PetugasMedisSatTugasType>> ListData(IPetugasMedisKey filter)
    {
        const string sql = @"
                 SELECT 
                    aa.fs_kd_peg, aa.fs_kd_sat_tugas, aa.fn_utama,
                    ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas,
	                ISNULL(bb.fb_sat_medis,'') AS fb_sat_medis 
                FROM 
                    td_peg_sat_tugas aa
                    LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
                 WHERE 
                     aa.fs_kd_peg = @fs_kd_peg";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PetugasMedisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.Read<PetugasMedisSatTugasDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));

        return datas;
    }
    //
    //     public void Insert(IEnumerable<PetugasMedisSatTugasType> listModel)
    //     {
    //         //  INSERT BULK
    //         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //         using var bcp = new SqlBulkCopy(conn);
    //         
    //         bcp.AddMap("PetugasMedisId", "fs_kd_peg");
    //         bcp.AddMap("SatTugasId", "fs_kd_sat_tugas");
    //         bcp.AddMap("IsUtama", "fn_utama");
    //
    //         conn.Open();
    //         var fetched = listModel.ToList();
    //         bcp.BatchSize = fetched.Count;
    //         bcp.DestinationTableName = "td_peg_sat_tugas";
    //         bcp.WriteToServer(fetched.AsDataTable());
    //     }
    //
    //     public void Delete(IPetugasMedisKey key)
    //     {
    //         const string sql = @"
    //             DELETE FROM 
    //                 td_peg_sat_tugas
    //             WHERE 
    //                 fs_kd_peg = @fs_kd_peg";
    //         
    //         var dp = new DynamicParameters();
    //         dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
    //         
    //         var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //         conn.Execute(sql, dp);
    //     }
    //
    //     public IEnumerable<PetugasMedisSatTugasType> ListData(IPetugasMedisKey filter)
    //     {
    //         const string sql = @"
    //             SELECT 
    //                 aa.fs_kd_peg, aa.fs_kd_sat_tugas, aa.fn_utama,
    //                 ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas
    //             FROM 
    //                 td_peg_sat_tugas aa
    //                 LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
    //             WHERE 
    //                 aa.fs_kd_peg = @fs_kd_peg";
    //         
    //         var dp = new DynamicParameters();
    //         dp.AddParam("@fs_kd_peg", filter.PetugasMedisId, SqlDbType.VarChar);
    //         
    //         var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //         return conn.Query<PetugasMedisSatTugasDto>(sql, dp);
    //     }
}
//
public class PetugasMedisSatTugasDto
{
    public string fs_kd_peg { get; set; }
    public string fs_kd_sat_tugas { get; set; }
    public string fs_nm_sat_tugas { get; set; }
    public int fn_utama { get; set; }
    public bool fb_sat_medis { get; set; }

    public PetugasMedisSatTugasType ToModel()
    {
        var satTugas = new SatTugasType(fs_kd_sat_tugas, fs_nm_sat_tugas, fb_sat_medis);
        bool isUtama = fn_utama == 0 ? true : false;

        return new PetugasMedisSatTugasType(satTugas, isUtama);
    }
}
//
// public class PetugasMedisSatTugasDalTest
// {
//     private readonly PetugasMedisSatTugasDal _sut;
//
//     public PetugasMedisSatTugasDalTest()
//     {
//         _sut = new PetugasMedisSatTugasDal(ConnStringHelper.GetTestEnv());
//     }
//
//     [Fact]
//     public void InsertTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var expected = new PetugasMedisSatTugasType("A", "B", "C");
//         expected.SetUtama();
//         _sut.Insert(new List<PetugasMedisSatTugasType> { expected });
//     }
//
//     [Fact]
//     public void DeleteTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var expected = new PetugasMedisSatTugasType("A", "B", "C");
//         expected.SetUtama();
//         _sut.Delete(expected);
//     }
//
//     [Fact]
//     public void ListDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var expected = new PetugasMedisSatTugasType("A", "B", "");
//         expected.SetUtama();
//         _sut.Insert(new List<PetugasMedisSatTugasType> { expected });
//         var actual = _sut.ListData(expected);
//         actual.Should().BeEquivalentTo(new List<PetugasMedisSatTugasType> { expected });
//     }
// }