using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public interface IPpaDal :
    IInsert<PpaDto>,
    IUpdate<PpaDto>,
    IDelete<IPpaKey>,
    IGetData<PpaDto, IPpaKey>,
    IListData<PpaDto>
{
}

public class PpaDal : IPpaDal
{
    private readonly DatabaseOptions _opt;

    public PpaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PpaDto dto)
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

    public void Update(PpaDto dto)
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

    public void Delete(IPpaKey key)
    {
        const string sql = """
            DELETE FROM 
                td_peg
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PpaId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PpaDto GetData(IPpaKey key)
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
        dp.AddParam("@fs_kd_peg", key.PpaId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PpaDto>(sql, dp);
    }

    public IEnumerable<PpaDto> ListData()
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
        return conn.Query<PpaDto>(sql).ToList();
    }
}
