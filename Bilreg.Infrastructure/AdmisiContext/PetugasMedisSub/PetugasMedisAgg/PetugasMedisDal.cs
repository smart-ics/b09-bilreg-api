using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public class PetugasMedisDal : IPetugasMedisDal
{
    private readonly DatabaseOptions _opt;

    public PetugasMedisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PetugasMedisModel model)
    {
        const string sql = @"
            INSERT INTO td_peg( fs_kd_peg, fs_nm_peg, fs_nm_alias, fs_kd_smf)
            VALUES( @fs_kd_peg, @fs_nm_peg, @fs_nm_alias, @fs_kd_smf )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", model.PetugasMedisId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_peg", model.PetugasMedisName, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_alias", model.NamaSingkat, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_smf", model.SmfId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PetugasMedisModel model)
    {
        const string sql = @"
            UPDATE 
                td_peg
            SET 
                fs_nm_peg = @fs_nm_peg, 
                fs_nm_alias = @fs_nm_alias, 
                fs_kd_smf = @fs_kd_smf
            WHERE 
                fs_kd_peg = @fs_kd_peg";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", model.PetugasMedisId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_peg", model.PetugasMedisName, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_alias", model.NamaSingkat, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_smf", model.SmfId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPetugasMedisKey key)
    {
        const string sql = @"
            DELETE FROM 
                td_peg
            WHERE 
                fs_kd_peg = @fs_kd_peg";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PetugasMedisModel GetData(IPetugasMedisKey key)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_peg, aa.fs_nm_peg, aa.fs_nm_alias, aa.fs_kd_smf,
                ISNULL(bb.fs_nm_smf, '') fs_nm_smf
            FROM 
                td_peg aa
                LEFT JOIN ta_smf bb ON aa.fs_kd_smf = bb.fs_kd_smf
            WHERE 
                fs_kd_peg = @fs_kd_peg";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PetugasMedisDto>(sql, dp);
    }

    public IEnumerable<PetugasMedisModel> ListData()
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_peg, aa.fs_nm_peg, aa.fs_nm_alias, aa.fs_kd_smf,
                ISNULL(bb.fs_nm_smf, '') fs_nm_smf
            FROM 
                td_peg aa
                LEFT JOIN ta_smf bb ON aa.fs_kd_smf = bb.fs_kd_smf
            ORDER BY 
                aa.fs_kd_peg";
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PetugasMedisDto>(sql).ToList();
    }
}

public class PetugasMedisDto() : PetugasMedisModel(string.Empty, string.Empty)
{
    public string fs_kd_peg { get => PetugasMedisId; set => PetugasMedisId = value; }
    public string fs_nm_peg { get => PetugasMedisName; set => PetugasMedisName = value; }
    public string fs_nm_alias { get => NamaSingkat; set => NamaSingkat = value; }
    public string fs_kd_smf { get => SmfId; set => SmfId = value; }
    public string fs_nm_smf { get => SmfName; set => SmfName = value; }
}

public class PetugasMedisDalTest
{
    private readonly PetugasMedisDal _sut;
    
    public PetugasMedisDalTest()
    {
        _sut = new PetugasMedisDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PetugasMedisModel("A", "B");
        var smf = SmfModel.Create("C", "D");
        expected.Set(smf);
        expected.SetNama("E", "B");
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PetugasMedisModel("A", "B");
        var smf = SmfModel.Create("C", "D");
        expected.Set(smf);
        expected.SetNama("E", "B");
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PetugasMedisModel("A", "B");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PetugasMedisModel("A", "B");
        var smf = SmfModel.Create("C", "");
        expected.Set(smf);
        expected.SetNama("E", "B");
        _sut.Insert(expected);
        
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PetugasMedisModel("A", "B");
        var smf = SmfModel.Create("C", "");
        expected.Set(smf);
        expected.SetNama("E", "B");
        _sut.Insert(expected);
        
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(new List<PetugasMedisModel>{expected});
    }
}