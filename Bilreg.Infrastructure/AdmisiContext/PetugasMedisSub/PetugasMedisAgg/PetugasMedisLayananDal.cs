using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public class PetugasMedisLayananDal : IPetugasMedisLayananDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisLayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PetugasMedisLayananType> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("PetugasMedisId", "fs_kd_peg");
        bcp.AddMap("LayananId", "fs_kd_layanan");
        bcp.AddMap("IsUtama", "fb_utama");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "td_peg_layanan";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPetugasMedisKey key)
    {
        const string sql = @"
             DELETE FROM 
                 td_peg_layanan
             WHERE 
                 fs_kd_peg = @fs_kd_peg";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<IEnumerable<PetugasMedisLayananType>> ListData(IPetugasMedisKey filter)
    {
        const string sql = @"
             SELECT 
                 aa.fs_kd_peg, aa.fs_kd_layanan, aa.fb_utama,
                 ISNULL(bb.fs_nm_layanan, '') AS fs_nm_layanan
             FROM 
                 td_peg_layanan aa
                 LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
             WHERE 
                 aa.fs_kd_peg = @fs_kd_peg ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PetugasMedisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        var datas = MayBe
            .From(conn.Read<PetugasMedisLayananDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
        return datas;
    }
}

public class PetugasMedisLayananDto
{
    public string fs_kd_peg { get; set; }
    public string fs_kd_layanan { get; set; }
    public string fs_nm_layanan { get; set; }
    public bool fb_utama { get; set; }
    public PetugasMedisLayananType ToModel()
    {
        var lynReff = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var ptgMdsLynType = new PetugasMedisLayananType(lynReff, fb_utama);
        return ptgMdsLynType;
    }
}
//
// public class PetugasMedisLayananTest
// {
//     private readonly PetugasMedisLayananDal _sut;
//
//     public PetugasMedisLayananTest()
//     {
//         _sut = new PetugasMedisLayananDal(ConnStringHelper.GetTestEnv());
//     }
//
//     [Fact]
//     public void InsertTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var expected = new PetugasMedisLayananType("A", "B", "C");
//         expected.SetUtama();
//         _sut.Insert(new List<PetugasMedisLayananType>{expected});
//     }
//     
//     [Fact]
//     public void DeleteTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var expected = new PetugasMedisLayananType("A", "B", "C");
//         expected.SetUtama();
//         _sut.Delete(expected);
//     }
//
//     [Fact]
//     public void ListDataTest()
//     {
//         using var trans = TransHelper.NewScope();
//         var expected = new PetugasMedisLayananType("A", "B", "");
//         expected.SetUtama();
//         _sut.Insert(new List<PetugasMedisLayananType>{expected});
//         var actual = _sut.ListData(expected);
//         actual.Should().BeEquivalentTo(new List<PetugasMedisLayananType>{expected});
//     }
// }