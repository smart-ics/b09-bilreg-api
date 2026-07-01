using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public interface IPpaSatTugasDal :
    IInsertBulk<PpaSatTugasDto>,
    IDelete<IPpaKey>,
    IListData<PpaSatTugasDto, IPpaKey>
{
}

public class PpaSatTugasDal : IPpaSatTugasDal
{
    private readonly DatabaseOptions _opt;

    public PpaSatTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PpaSatTugasDto> listModel)
    {
        //  INSERT BULK
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        bcp.AddMap("fs_kd_peg", "fs_kd_peg");
        bcp.AddMap("fs_kd_sat_tugas", "fs_kd_sat_tugas");
        bcp.AddMap("fn_utama", "fn_utama");

        conn.Open();
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "td_peg_sat_tugas";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPpaKey key)
    {
        const string sql = """
            DELETE FROM 
                td_peg_sat_tugas
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PpaId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PpaSatTugasDto> ListData(IPpaKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_sat_tugas, aa.fn_utama, 
                ISNULL(bb.fs_nm_sat_tugas, '') AS fs_nm_sat_tugas,
                ISNULL(bb.fs_kd_profesi, '') AS fs_kd_profesi,
                ISNULL(cc.ProfesiName, '') AS fs_nm_profesi
            FROM 
                td_peg_sat_tugas aa
                LEFT JOIN td_sat_tugas bb ON aa.fs_kd_sat_tugas = bb.fs_kd_sat_tugas
                LEFT JOIN BILRG_Profesi cc ON bb.fs_kd_profesi = cc.ProfesiId    
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PpaId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PpaSatTugasDto>(sql, dp);
    }
}
