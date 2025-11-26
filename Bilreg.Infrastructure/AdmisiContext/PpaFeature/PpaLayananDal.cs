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

public interface IPpaLayananDal : 
    IInsertBulk<PpaLayananDto>,
    IDelete<IPpaKey>,
    IListData<PpaLayananDto, IPpaKey>,
    IListData<PpaLayananView, ISatTugasKey, IInstalasiDkKey>,
    IListData<PpaLayananView, ISatTugasKey>
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
        bcp.AddMap("PetugasMedisId", "fs_kd_peg");
        bcp.AddMap("LayananId", "fs_kd_layanan");
        bcp.AddMap("IsUtama", "fb_utama");

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
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PpaLayananDto> ListData(IPpaKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_layanan, aa.fb_utama,
                ISNULL(bb.fs_nm_layanan, '') AS fs_nm_layanan
            FROM 
                td_peg_layanan aa
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
            WHERE 
                aa.fs_kd_peg = @fs_kd_peg
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_peg", filter.PpaId, SqlDbType.VarChar);
        
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PpaLayananDto>(sql, dp);
    }

    public IEnumerable<PpaLayananView> ListData(ISatTugasKey satTgsKey, IInstalasiDkKey instalasiDkKey)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_peg, aa.fs_kd_layanan, aa.fb_utama,
                ISNULL(bb.fs_nm_layanan, '') AS fs_nm_layanan,
                ISNULL(cc.fs_nm_peg,'') AS fs_nm_peg,
                ISNULL(bb.GroupSpesialisId,'') AS GroupSpesialisId,
                ISNULL(dd.GroupSpesialisName,'') AS GroupSpesialisName
            FROM 
                td_peg_layanan aa
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan 
                INNER JOIN td_peg cc ON aa.fs_kd_peg = cc.fs_kd_peg AND cc.fb_aktif_dinas = 1 
                LEFT JOIN BILRG_GroupSpesialis dd ON bb.GroupSpesialisId = dd.GroupSpesialisId
                LEFT JOIN ta_instalasi ee ON bb.fs_kd_instalasi = ee.fs_kd_instalasi 
            WHERE 
                cc.fs_kd_sat_tugas = @SatTugasMedisId
                AND ee.fs_kd_instalasi_dk = @InstalasiDkId 
                AND bb.FB_AKTIF = 1
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@SatTugasMedisId", satTgsKey.SatTugasId, SqlDbType.VarChar);
        dp.AddParam("@InstalasiDkId", instalasiDkKey.InstalasiDkId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PpaLayananView>(sql, dp);
    }

    public IEnumerable<PpaLayananView> ListData(ISatTugasKey filter)
    {
        const string sql = """
            SELECT
                aa.fs_kd_peg, aa.fs_kd_layanan, aa.fb_utama,
                ISNULL(bb.fs_nm_layanan, '') AS fs_nm_layanan,
                ISNULL(cc.fs_nm_peg,'') AS fs_nm_peg,
                ISNULL(bb.GroupSpesialisId,'') AS GroupSpesialisId,
                ISNULL(dd.GroupSpesialisName,'') AS GroupSpesialisName
            FROM
                td_peg_layanan aa
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
                LEFT JOIN td_peg cc ON aa.fs_kd_peg = cc.fs_kd_peg AND cc.fb_aktif_dinas = 1
                LEFT JOIN BILRG_GroupSpesialis dd ON bb.GroupSpesialisId = dd.GroupSpesialisId
            WHERE
                cc.fs_kd_sat_tugas = @SatTugasMedisId
                AND bb.fb_aktif = 1
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@SatTugasMedisId", filter.SatTugasId, SqlDbType.VarChar);

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PpaLayananView>(sql, dp);
    }
}
