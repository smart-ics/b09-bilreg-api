using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
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
        bcp.AddMap("fd_tgl_expired", "FD_TGL_EXPIRED");
        bcp.AddMap("crttgl", "CRTTGL");
        bcp.AddMap("crtjam", "CRTJAM");
        bcp.AddMap("crtipa", "CRTIPA");
        bcp.AddMap("crtver", "CRTVER");
        bcp.AddMap("crtusr", "CRTUSR");
        bcp.AddMap("updtgl", "UPDTGL");
        bcp.AddMap("updjam", "UPDJAM");
        bcp.AddMap("updipa", "UPDIPA");
        bcp.AddMap("updver", "UPDVER");
        bcp.AddMap("updusr", "UPDUSR");

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
                ISNULL(aa.fs_kd_polis, '') AS fs_kd_polis,
                ISNULL(aa.fs_mr, '') AS fs_mr,
                ISNULL(aa.fs_kd_status, '') AS fs_kd_status,
                ISNULL(bb.fs_nm_pasien, '') AS fs_nm_pasien,
                ISNULL(bb.fd_tgl_lahir, '') AS fd_tgl_lahir,
                ISNULL(bb.fs_jns_kelamin, '') AS fs_jns_kelamin,
                ISNULL(aa.fd_tgl_expired, '3000-01-01') AS fd_tgl_expired,
                ISNULL(aa.crttgl, '3000-01-01') AS crttgl,
                ISNULL(aa.crtjam, '00:00:00') AS crtjam,
                ISNULL(aa.crtipa, '') AS crtipa,
                ISNULL(aa.crtver, '') AS crtver,
                ISNULL(aa.crtusr, '') AS crtusr,
                ISNULL(aa.updtgl, '3000-01-01') AS updtgl,
                ISNULL(aa.updjam, '00:00:00') AS updjam,
                ISNULL(aa.updipa, '') AS updipa,
                ISNULL(aa.updver, '') AS updver,
                ISNULL(aa.updusr, '') AS updusr
            FROM ta_polis_cover aa
                LEFT JOIN tc_mr bb ON aa.fs_mr = bb.fs_mr
            WHERE aa.fs_kd_polis = @fs_kd_polis
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_polis", filter.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PolisCoverDto>(sql, dp);
    }
}