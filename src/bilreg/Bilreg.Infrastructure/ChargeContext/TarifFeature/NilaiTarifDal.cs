using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface INilaiTarifDal :
    IInsert<NilaiTarifDto>,
    IInsertBulk<NilaiTarifDto>,
    IUpdate<NilaiTarifDto>,
    IDelete<INilaiTarifKey>,
    IGetData<NilaiTarifDto, INilaiTarifKey>,
    IListData<NilaiTarifDto, ITarifKey>
{
    IEnumerable<ta_trs_tarif2_dto> ListData2(DateOnly businessDate);
    IEnumerable<ta_trs_tarif3_dto> ListData3();
    void Clear();
    IEnumerable<NilaiTarifDto> ListData(ILayananKey lyn, INilaiTarifVariant variant, string keywprd);

    IEnumerable<NilaiTarifDto> ListAllHeaders();

    NilaiTarifProjectionSummaryRow GetProjectionSummary();

    NilaiTarifProjectionConsistencyRow GetConsistencyCounts();
}

public class NilaiTarifDal : INilaiTarifDal
{
    private readonly DatabaseOptions _opt;

    public NilaiTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(NilaiTarifDto dto)
    {
        const string sql = """
           INSERT INTO BILRG_NilaiTarif(
               NilaiTarifId, TarifId, TipeTarifId, KelasId, Nilai, SourcePolicyId)
           VALUES( 
               @NilaiTarifId, @TarifId, @TipeTarifId, @KelasId, @Nilai, @SourcePolicyId)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", dto.NilaiTarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", dto.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@Nilai", dto.Nilai, SqlDbType.Decimal);
        dp.AddParam("@SourcePolicyId", dto.SourcePolicyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public void Insert(IEnumerable<NilaiTarifDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("NilaiTarifId", "NilaiTarifId");
        bcp.AddMap("TarifId", "TarifId");
        bcp.AddMap("TipeTarifId", "TipeTarifId");
        bcp.AddMap("KelasId", "KelasId");
        bcp.AddMap("Nilai", "Nilai");
        bcp.AddMap("SourcePolicyId", "SourcePolicyId");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_NilaiTarif";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Update(NilaiTarifDto dto)
    {
        const string sql = """
           UPDATE 
               BILRG_NilaiTarif
           SET
               TarifId = @TarifId,
               TipeTarifId = @TipeTarifId,
               KelasId = @KelasId,
               Nilai = @Nilai,
               SourcePolicyId = @SourcePolicyId
           WHERE
               NilaiTarifId = @NilaiTarifId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", dto.NilaiTarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", dto.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("@Nilai", dto.Nilai, SqlDbType.Decimal);
        dp.AddParam("@SourcePolicyId", dto.SourcePolicyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(INilaiTarifKey key)
    {
        const string sql = """
           DELETE FROM 
                BILRG_NilaiTarif
           WHERE
               NilaiTarifId = @NilaiTarifId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", key.NilaiTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Clear()
    {
        const string sql = "DELETE FROM BILRG_NilaiTarif";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql);
    }

    public NilaiTarifDto GetData(INilaiTarifKey key)
    {
        const string sql = """
           SELECT
               aa.NilaiTarifId, aa.TarifId, aa.TipeTarifId, aa.KelasId, aa.Nilai,
               ISNULL(bb.fs_nm_tarif, '') AS TarifName,
               ISNULL(cc.fs_nm_tarif_tipe, '') AS TipeTarifName,
               ISNULL(dd.fs_nm_kelas, '') AS KelasName,
               ISNULL(aa.SourcePolicyId, '') AS SourcePolicyId
           FROM 
               BILRG_NilaiTarif aa
               LEFT JOIN ta_tarif bb ON aa.TarifId = bb.fs_kd_tarif
               LEFT JOIN ta_tarif_tipe cc ON aa.TipeTarifId = cc.fs_kd_tarif_tipe
               LEFT JOIN ta_kelas dd ON aa.KelasId = dd.fs_kd_kelas
           WHERE
               aa.NilaiTarifId = @NilaiTarifId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", key.NilaiTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<NilaiTarifDto>(sql, dp);
        return result;
    }

    public IEnumerable<NilaiTarifDto> ListData(ITarifKey key)
    {
        const string sql = """
            SELECT
                aa.NilaiTarifId, aa.TarifId, aa.TipeTarifId, aa.KelasId, aa.Nilai,
                ISNULL(bb.fs_nm_tarif, '') AS TarifName,
                ISNULL(cc.fs_nm_tarif_tipe, '') AS TipeTarifName,
                ISNULL(dd.fs_nm_kelas, '') AS KelasName,
                ISNULL(aa.SourcePolicyId, '') AS SourcePolicyId
            FROM 
                BILRG_NilaiTarif aa
                LEFT JOIN ta_tarif bb ON aa.TarifId = bb.fs_kd_tarif
                LEFT JOIN ta_tarif_tipe cc ON aa.TipeTarifId = cc.fs_kd_tarif_tipe
                LEFT JOIN ta_kelas dd ON aa.KelasId = dd.fs_kd_kelas
            WHERE
                aa.TarifId = @TarifId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TarifId", key.TarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifDto>(sql, dp);
    }

    public IEnumerable<NilaiTarifDto> ListData(ILayananKey layanan, 
        INilaiTarifVariant variant, string keyword)
    {
        const string sql = """
           SELECT
               aa.NilaiTarifId, aa.TarifId, aa.TipeTarifId, aa.KelasId, aa.Nilai,
               ISNULL(bb.fs_nm_tarif, '') AS TarifName,
               ISNULL(cc.fs_nm_tarif_tipe, '') AS TipeTarifName,
               ISNULL(dd.fs_nm_kelas, '') AS KelasName,
               ISNULL(aa.SourcePolicyId, '') AS SourcePolicyId
           FROM 
               BILRG_NilaiTarif aa
               LEFT JOIN ta_tarif bb ON aa.TarifId = bb.fs_kd_tarif
               LEFT JOIN ta_tarif_tipe cc ON aa.TipeTarifId = cc.fs_kd_tarif_tipe
               LEFT JOIN ta_kelas dd ON aa.KelasId = dd.fs_kd_kelas
               LEFT JOIN ta_tarif4 ee ON aa.TarifId = ee.fs_kd_tarif
           WHERE
               ee.fs_kd_layanan = @LayananId
               AND aa.TipeTarifId = @TipeTarifId
               AND aa.KelasId = @KelasId
               AND bb.fs_nm_tarif LIKE @Keyword
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@LayananId", layanan.LayananId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", variant.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@KelasId", variant.KelasId, SqlDbType.VarChar);
        dp.AddParam("@Keyword", $"%{keyword}%", SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifDto>(sql, dp);
    }

    public IEnumerable<ta_trs_tarif2_dto> ListData2(DateOnly businessDate)
    {
        const string sql = """
           SELECT fs_kd_trs, fs_kd_tarif
           FROM ta_trs_tarif2
           WHERE fd_tgl_expired <= @fd_tgl_now
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fd_tgl_now", businessDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ta_trs_tarif2_dto>(sql,dp);
    }

    public IEnumerable<ta_trs_tarif3_dto> ListData3()
    {
        const string sql = """
            SELECT 
                aa.fs_kd_trs, aa.fs_kd_tarif, aa.fs_kd_kelas, 
                aa.fs_kd_tipe, aa.fs_kd_detil, aa.fn_nilai
            FROM 
                ta_trs_tarif3 aa
                INNER JOIN ta_trs_tarif2 bb ON aa.fs_kd_trs = bb.fs_kd_trs 
                    AND aa.fs_kd_tarif = bb.fs_kd_tarif
            WHERE 
                bb.fd_tgl_expired = '3000-01-01'
                AND aa.fn_nilai > 0
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ta_trs_tarif3_dto>(sql);
    }

    public IEnumerable<NilaiTarifDto> ListAllHeaders()
    {
        const string sql = """
            SELECT
                NilaiTarifId, TarifId, TipeTarifId, KelasId, Nilai,
                '' AS TarifName, '' AS TipeTarifName, '' AS KelasName,
                ISNULL(SourcePolicyId, '') AS SourcePolicyId
            FROM BILRG_NilaiTarif
            ORDER BY TarifId, KelasId, TipeTarifId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifDto>(sql);
    }

    public NilaiTarifProjectionSummaryRow GetProjectionSummary()
    {
        const string sql = """
            SELECT
                COUNT(*) AS TotalVariantCount,
                SUM(CASE WHEN ISNULL(SourcePolicyId, '') = '' THEN 1 ELSE 0 END) AS ImportOnlyCount,
                SUM(CASE WHEN ISNULL(SourcePolicyId, '') <> '' THEN 1 ELSE 0 END) AS PolicySourcedCount
            FROM BILRG_NilaiTarif
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifProjectionSummaryRow>(sql).First();
    }

    public NilaiTarifProjectionConsistencyRow GetConsistencyCounts()
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM (
                    SELECT TarifId, TipeTarifId, KelasId
                    FROM BILRG_NilaiTarif
                    GROUP BY TarifId, TipeTarifId, KelasId
                    HAVING COUNT(*) > 1
                ) dup) AS DuplicateVariantKeyCount,
                (SELECT COUNT(*)
                 FROM BILRG_NilaiTarif nt
                 WHERE NOT EXISTS (
                     SELECT 1 FROM BILRG_NilaiTarifKomponen k
                     WHERE k.NilaiTarifId = nt.NilaiTarifId)) AS HeadersWithoutKomponenCount,
                (SELECT COUNT(*)
                 FROM BILRG_NilaiTarif nt
                 WHERE ISNULL(nt.SourcePolicyId, '') <> ''
                   AND NOT EXISTS (
                     SELECT 1 FROM BILRG_TarifPolicy p
                     WHERE p.TarifPolicyId = nt.SourcePolicyId)) AS OrphanSourcePolicyIdCount
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifProjectionConsistencyRow>(sql).First();
    }
}
