using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public class PetugasMedisDal : IPetugasMedisDal
{
    private readonly DatabaseOptions _opt;
    private readonly PetugasMedisOptions _ptgMedisOpt;
    public PetugasMedisDal(IOptions<DatabaseOptions> opt, 
        IOptions<PetugasMedisOptions> ptgMedisOpt)
    {
        _opt = opt.Value;
        _ptgMedisOpt = ptgMedisOpt.Value;
    }

    public void Insert(PetugasMedisType model)
    {
        throw new NotImplementedException();
    }

    public void Update(PetugasMedisType model)
    {
        throw new NotImplementedException();
    }

    public void Delete(IPetugasMedisKey key)
    {
        throw new NotImplementedException();
    }

    public MayBe<PetugasMedisType> GetData(IPetugasMedisKey key)
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

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var data = MayBe
            .From(conn.ReadSingle<PetugasMedisDto>(sql, dp))
            .Map(x => x.ToModel());

        return data;
    }

    public MayBe<IEnumerable<PetugasMedisType>> ListData()
    {
        const string sql = @"
             SELECT 
                aa.fs_kd_peg, aa.fs_nm_peg, aa.fs_nm_alias, aa.fs_kd_smf,
                ISNULL(bb.fs_nm_smf, '') fs_nm_smf, cc.FS_KD_SAT_TUGAS
            FROM 
                td_peg aa
                LEFT JOIN ta_smf bb ON aa.fs_kd_smf = bb.fs_kd_smf
	            INNER JOIN td_peg_sat_tugas cc ON aa.fs_kd_peg = cc.FS_KD_PEG 
		            AND cc.fs_kd_sat_tugas = @SatTugas
            WHERE 
	            aa.fb_aktif_dinas = 1";

        var dp = new DynamicParameters();
        dp.AddParam("@SatTugas", _ptgMedisOpt.KodeSatuanTugasMedis, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.Read<PetugasMedisDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));

        return datas;
    }

    //public void Insert(PetugasMedisModel model)
    //{
    //    const string sql = @"
    //         INSERT INTO td_peg( fs_kd_peg, fs_nm_peg, fs_nm_alias, fs_kd_smf)
    //         VALUES( @fs_kd_peg, @fs_nm_peg, @fs_nm_alias, @fs_kd_smf )";

    //    var dp = new DynamicParameters();
    //    dp.AddParam("@fs_kd_peg", model.PetugasMedisId, SqlDbType.VarChar);
    //    dp.AddParam("@fs_nm_peg", model.PetugasMedisName, SqlDbType.VarChar);
    //    dp.AddParam("@fs_nm_alias", model.NamaSingkat, SqlDbType.VarChar);
    //    dp.AddParam("@fs_kd_smf", model.SmfId, SqlDbType.VarChar);

    //    var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //    conn.Execute(sql, dp);
    //}

    //public void Update(PetugasMedisModel model)
    //{
    //    const string sql = @"
    //         UPDATE 
    //             td_peg
    //         SET 
    //             fs_nm_peg = @fs_nm_peg, 
    //             fs_nm_alias = @fs_nm_alias, 
    //             fs_kd_smf = @fs_kd_smf
    //         WHERE 
    //             fs_kd_peg = @fs_kd_peg";

    //    var dp = new DynamicParameters();
    //    dp.AddParam("@fs_kd_peg", model.PetugasMedisId, SqlDbType.VarChar);
    //    dp.AddParam("@fs_nm_peg", model.PetugasMedisName, SqlDbType.VarChar);
    //    dp.AddParam("@fs_nm_alias", model.NamaSingkat, SqlDbType.VarChar);
    //    dp.AddParam("@fs_kd_smf", model.SmfId, SqlDbType.VarChar);

    //    var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //    conn.Execute(sql, dp);
    //}

    //public void Delete(IPetugasMedisKey key)
    //{
    //    const string sql = @"
    //         DELETE FROM 
    //             td_peg
    //         WHERE 
    //             fs_kd_peg = @fs_kd_peg";

    //    var dp = new DynamicParameters();
    //    dp.AddParam("@fs_kd_peg", key.PetugasMedisId, SqlDbType.VarChar);

    //    var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //    conn.Execute(sql, dp);
    //}

    
}

public class PetugasMedisDto
{
    public string fs_kd_peg { get; set; }
    public string fs_nm_peg { get; set; }
    public string fs_nm_alias { get; set; }
    public string fs_kd_smf { get; set; }
    public string fs_nm_smf { get; set; }

    public PetugasMedisType ToModel()
    {
        var smf = new SmfType(fs_kd_smf, fs_nm_smf);
        return new PetugasMedisType(fs_kd_peg, fs_nm_peg, fs_nm_alias, smf,
            new List<PetugasMedisLayananType>(), new List<PetugasMedisSatTugasType>());
    }
}

//public class PetugasMedisDalTest
//{
//    private readonly PetugasMedisDal _sut;

//    public PetugasMedisDalTest()
//    {
//        _sut = new PetugasMedisDal(ConnStringHelper.GetTestEnv());
//    }

//    [Fact]
//    public void InsertTest()
//    {
//        using var trans = TransHelper.NewScope();
//        var expected = new PetugasMedisModel("A", "B");
//        var smf = SmfModel.Create("C", "D");
//        expected.Set(smf);
//        expected.SetNama("E", "B");
//        _sut.Insert(expected);
//    }

//    [Fact]
//    public void UpdateTest()
//    {
//        using var trans = TransHelper.NewScope();
//        var expected = new PetugasMedisModel("A", "B");
//        var smf = SmfModel.Create("C", "D");
//        expected.Set(smf);
//        expected.SetNama("E", "B");
//        _sut.Update(expected);
//    }

//    [Fact]
//    public void DeleteTest()
//    {
//        using var trans = TransHelper.NewScope();
//        var expected = new PetugasMedisModel("A", "B");
//        _sut.Delete(expected);
//    }

//    [Fact]
//    public void GetDataTest()
//    {
//        using var trans = TransHelper.NewScope();
//        var expected = new PetugasMedisModel("A", "B");
//        var smf = SmfModel.Create("C", "");
//        expected.Set(smf);
//        expected.SetNama("E", "B");
//        _sut.Insert(expected);

//        var actual = _sut.GetData(expected);
//        actual.Should().BeEquivalentTo(expected);
//    }

//    [Fact]
//    public void ListDataTest()
//    {
//        using var trans = TransHelper.NewScope();
//        var expected = new PetugasMedisModel("A", "B");
//        var smf = SmfModel.Create("C", "");
//        expected.Set(smf);
//        expected.SetNama("E", "B");
//        _sut.Insert(expected);

//        var actual = _sut.ListData();
//        actual.Should().BeEquivalentTo(new List<PetugasMedisModel> { expected });
//    }
//}