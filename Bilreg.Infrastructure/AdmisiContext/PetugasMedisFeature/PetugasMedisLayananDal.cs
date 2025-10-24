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

public interface IPetugasMedisLayananDal : 
    IInsertBulk<PetugasMedisLayananDto>,
    IDelete<IPetugasMedisKey>,
    IListData<PetugasMedisLayananDto, IPetugasMedisKey>
{
}

public class PetugasMedisLayananDal : IPetugasMedisLayananDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisLayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PetugasMedisLayananDto> listModel)
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
        const string sql = """
            DELETE FROM 
                td_peg_layanan
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PetugasMedisLayananDto> ListData(IPetugasMedisKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_layanan, aa.fb_utama,
                ISNULL(bb.fs_nm_layanan, '') AS fs_nm_layanan
            FROM 
                td_peg_layanan aa
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PetugasMedisLayananDto>(sql, dp);
    }
}



public class PetugasMedisLayananTest
{
    private readonly PetugasMedisLayananDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PetugasMedisLayananDto Faker()
        => new PetugasMedisLayananDto("A", "B", "C", true);
    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new List<PetugasMedisLayananDto>{Faker()});
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
        _sut.Insert(new List<PetugasMedisLayananDto>{Faker()});
        var actual = _sut.ListData(PetugasMedisType.Key("A"));
        actual.Should().ContainEquivalentOf(Faker());
    }
}