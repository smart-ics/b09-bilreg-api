using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public interface IPpaLayananDal : 
    IInsertBulk<PpaLayananDto>,
    IDelete<IPpaKey>,
    IListData<PpaLayananDto, IPpaKey>
{
}

public class PpaLayananDal : IPpaLayananDal
{
    private readonly DatabaseOptions _opt;

    public PpaLayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PpaLayananDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("fs_kd_peg", "fs_kd_peg");
        bcp.AddMap("fs_kd_layanan", "fs_kd_layanan");
        bcp.AddMap("fb_utama", "fb_utama");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "td_peg_layanan";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPpaKey key)
    {
        const string sql = """
            DELETE FROM 
                td_peg_layanan
            WHERE 
                fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", key.PpaId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PpaLayananDto> ListData(IPpaKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_layanan, aa.fb_utama,
                ISNULL(bb.fs_nm_peg, '') AS fs_nm_peg,
                ISNULL(cc.fs_nm_layanan, '') AS fs_nm_layanan
            FROM 
                td_peg_layanan aa
                LEFT JOIN td_peg bb ON aa.fs_kd_peg = bb.fs_kd_peg
                LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PpaId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PpaLayananDto>(sql, dp);
    }
}
