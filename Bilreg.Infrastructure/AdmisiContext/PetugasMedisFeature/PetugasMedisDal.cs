using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisFeature;

public interface IPetugasMedisDal :
    IInsert<PetugasMedisDto>,
    IUpdate<PetugasMedisDto>,
    IDelete<IPetugasMedisKey>,
    IGetData<PetugasMedisDto, IPetugasMedisKey>,
    IListData<PetugasMedisDto>
{
}

public class PetugasMedisDal : IPetugasMedisDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PetugasMedisDto dto)
    {
        const string sql = """
            INSERT INTO td_peg( fs_kd_peg, fs_nm_peg, fs_nm_alias, fs_kd_smf)
            VALUES( @fs_kd_peg, @fs_nm_peg, @fs_nm_alias, @fs_kd_smf )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", dto.fs_kd_peg, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_peg", dto.fs_nm_peg, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_alias", dto.fs_nm_alias, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_smf", dto.fs_kd_smf, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PetugasMedisDto dto)
    {
        const string sql = """
            UPDATE 
                td_peg
            SET 
                fs_nm_peg = @fs_nm_peg, 
                fs_nm_alias = @fs_nm_alias, 
                fs_kd_smf = @fs_kd_smf
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", dto.fs_kd_peg, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_peg", dto.fs_nm_peg, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_alias", dto.fs_nm_alias, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_smf", dto.fs_kd_smf, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPetugasMedisKey key)
    {
        const string sql = """
            DELETE FROM 
                td_peg
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PetugasMedisDto GetData(IPetugasMedisKey key)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_nm_peg, aa.fs_nm_alias, aa.fs_kd_smf,
                ISNULL(bb.fs_nm_smf, '') fs_nm_smf
            FROM 
                td_peg aa
                LEFT JOIN ta_smf bb ON aa.fs_kd_smf = bb.fs_kd_smf
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PetugasMedisDto>(sql, dp);
    }

    public IEnumerable<PetugasMedisDto> ListData()
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_nm_peg, aa.fs_nm_alias, aa.fs_kd_smf,
                ISNULL(bb.fs_nm_smf, '') fs_nm_smf
            FROM 
                td_peg aa
                LEFT JOIN ta_smf bb ON aa.fs_kd_smf = bb.fs_kd_smf
            WHERE 
                aa.fb_aktif_Dinas = 1
            ORDER BY 
                aa.fs_kd_peg
            """;
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PetugasMedisDto>(sql).ToList();
    }
}

public class PetugasMedisDalTest
{
    private readonly PetugasMedisDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PetugasMedisDto Faker()
        => new PetugasMedisDto("A", "B", "C", "D", "E");

    private static IPetugasMedisKey FakerKey()
        => PetugasMedisType.Default with { PetugasMedisId = "A" };

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
        actual.Should().BeEquivalentTo(Faker(), 
            opt => opt.Excluding(x => x.fs_nm_smf));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt.Excluding(x => x.fs_nm_smf));
    }
}