using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
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
    IListData<PpaLayananDto, IProfesiKey, IEnumerable<ILayananKey>>
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

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
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
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
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
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
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
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
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
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PpaDto>(sql).ToList();
    }

    public IEnumerable<PpaLayananDto> ListData(IProfesiKey filter, IEnumerable<ILayananKey> filter2)
    {
        const string sql = """
           SELECT 
               aa.fs_kd_peg, aa.fs_nm_peg,
               ISNULL(bb.fs_kd_layanan, '') fs_kd_layanan,
               ISNULL(bb.fb_utama, 0) fb_utama,
               ISNULL(cc.fs_nm_layanan, '') fs_nm_layanan
           FROM 
               td_peg aa
               LEFT JOIN td_peg_layanan bb ON aa.fs_kd_peg = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON bb.fs_kd_layanan = cc.fs_kd_layanan
               LEFT JOIN td_peg_sat_tugas dd ON aa.fs_kd_peg = dd.fs_kd_peg
               LEFT JOIN td_sat_tugas ee ON dd.fs_kd_sat_tugas = ee.fs_kd_sat_tugas
           WHERE 
               aa.fb_aktif_Dinas = 1
               AND ee.fs_kd_profesi = @fs_kd_profesi
               AND bb.fs_kd_layanan IN @ListLayananId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_profesi", filter.ProfesiId, SqlDbType.VarChar);
        dp.AddParam("@ListLayananId", filter2.Select(x => x.LayananId).ToList(), SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PpaLayananDto>(sql, dp);
    }

}
