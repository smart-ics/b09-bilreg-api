using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

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
        var result = conn.Query<PetugasMedisDto>(sql, dp).FirstOrDefault();
        return result?.ToModel();
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
        var result = conn.Query<PetugasMedisDto>(sql).ToList();
        return result.Select(x => x.ToModel());
    }
}

public class PetugasMedisDto
{
    public string fs_kd_peg { get; set; }
    public string fs_nm_peg { get; set; }
    public string fs_nm_alias { get; set; }
    public string fs_kd_smf { get; set; }
    public string fs_nm_smf { get; set; }
    
    public PetugasMedisModel ToModel()
    {
        var result = new PetugasMedisModel(fs_kd_peg, fs_nm_peg); 
        result.Set(SmfModel.Create(fs_kd_smf, fs_nm_smf));
        return result;
    }
}