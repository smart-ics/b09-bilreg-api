using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

// resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public interface IPolisCoverDal :
    IInsertBulk<PolisCoverDto>,
    IDelete<IPolisKey>,
    IListData<PolisCoverDto, IPolisKey>
{
}

public class PolisCoverDal : IPolisCoverDal
{
    private readonly DatabaseOptions _opt;

    public PolisCoverDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PolisCoverDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("fs_kd_polis", "FS_KD_POLIS");
        bcp.AddMap("fs_mr", "FS_MR");
        bcp.AddMap("fs_kd_status", "FS_KD_STATUS");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_polis_cover";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPolisKey key)
    {
        const string sql = @"
            DELETE FROM ta_polis_cover
            WHERE fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PolisCoverDto> ListData(IPolisKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_polis, aa.fs_mr, aa.fs_kd_status,
                ISNULL(bb.fs_nm_pasien, '') fs_nm_pasien,
                ISNULL(bb.fd_tgl_lahir, '') fd_tgl_lahir,
                ISNULL(bb.fs_jns_kelamin, '') fs_jns_kelamin
            FROM ta_polis_cover aa
                LEFT JOIN tc_mr bb ON aa.fs_mr = bb.fs_mr
            WHERE fs_kd_polis = @fs_kd_polis
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_polis", filter.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PolisCoverDto>(sql, dp);
    }
}