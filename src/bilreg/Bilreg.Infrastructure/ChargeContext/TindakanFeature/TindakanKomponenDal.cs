using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface ITindakanKomponenDal :
    IInsertBulk<TindakanKomponenDto>,
    IDelete<ITindakanKey>,
    IListData<TindakanKomponenDto, ITindakanKey>
{
    void UpdatePetugas(string tindakanId, string ppaId, string userId);
    string GetPetugasName(string ppaId);
}

public class TindakanKomponenDal : ITindakanKomponenDal
{
    private readonly DatabaseOptions _opt;

    public TindakanKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }

    public void Insert(IEnumerable<TindakanKomponenDto> listModel)
    {
        var list = listModel.ToList();
        if (list.Count == 0) return;

        const string sql = """
            INSERT INTO TA_TRS_TINDAKAN2(
                FS_KD_TRS, FS_KD_DETIL_TARIF, FN_TARIF, FN_JML, FN_SUB_TOTAL, FN_DISKON, FN_TOTAL,
                FS_KD_PETUGAS_MEDIS, FB_VOID, FS_KD_REK_PDPT, FS_KD_REK_DISKON, FS_KD_PETUGAS_EDIT_MEDIS,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR, UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            )
            VALUES(
                @FS_KD_TRS, @FS_KD_DETIL_TARIF, @FN_TARIF, @FN_JML, @FN_SUB_TOTAL, 0, @FN_TOTAL,
                @FS_KD_PETUGAS_MEDIS, 0, '', '', '',
                @CRTTGL, @CRTJAM, '', '', @CRTUSR, '3000-01-01', '00:00:00', '', '', ''
            )
            """;

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var nowTime = DateTime.Now.ToString("HH:mm:ss");

        var entries = list.Select(x => new
        {
            FS_KD_TRS = SafeTruncate(x.TindakanId, 10),
            FS_KD_DETIL_TARIF = SafeTruncate(x.KomponenTarifId, 3),
            FN_TARIF = x.Nilai,
            FN_JML = x.Qty,
            FN_SUB_TOTAL = x.SubTotal,
            FN_TOTAL = x.SubTotal,
            FS_KD_PETUGAS_MEDIS = SafeTruncate(x.PpaId, 10),
            CRTTGL = today,
            CRTJAM = nowTime,
            CRTUSR = ""
        });

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, entries);
    }

    public void Delete(ITindakanKey key)
    {
        const string sql = """
            DELETE FROM
                TA_TRS_TINDAKAN2
            WHERE
                FS_KD_TRS = @FS_KD_TRS
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_TRS", key.TindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TindakanKomponenDto> ListData(ITindakanKey filter)
    {
        const string sql = """
            SELECT
                k.FS_KD_TRS AS TindakanId,
                CAST(ROW_NUMBER() OVER(ORDER BY k.FS_KD_DETIL_TARIF) AS INT) AS NoUrut,
                k.FS_KD_DETIL_TARIF AS KomponenTarifId,
                ISNULL(d.FS_NM_DETIL_TARIF, '') AS KomponenTarifName,
                ISNULL(k.FS_KD_PETUGAS_MEDIS, '') AS PpaId,
                ISNULL(p.FS_NM_PEG, '') AS PpaName,
                k.FN_JML AS Qty,
                k.FN_TARIF AS Nilai,
                k.FN_SUB_TOTAL AS SubTotal
            FROM
                TA_TRS_TINDAKAN2 k
                LEFT JOIN TA_DETIL_TARIF d ON k.FS_KD_DETIL_TARIF = d.FS_KD_DETIL_TARIF
                LEFT JOIN td_peg p ON k.FS_KD_PETUGAS_MEDIS = p.FS_KD_PEG
            WHERE
                k.FS_KD_TRS = @FS_KD_TRS
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_TRS", filter.TindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanKomponenDto>(sql, dp);
    }

    public void UpdatePetugas(string tindakanId, string ppaId, string userId)
    {
        const string sql = """
            DECLARE @Today VARCHAR(10) = CONVERT(VARCHAR(10), GETDATE(), 120);
            DECLARE @NowTime VARCHAR(8) = CONVERT(VARCHAR(8), GETDATE(), 108);

            -- 1. Update TA_TRS_TINDAKAN2
            IF EXISTS (
                SELECT 1 FROM TA_TRS_TINDAKAN2 
                WHERE FS_KD_TRS = @TindakanId AND ISNULL(FS_KD_PETUGAS_MEDIS, '') <> ''
            )
            BEGIN
                UPDATE TA_TRS_TINDAKAN2
                SET 
                    FS_KD_PETUGAS_MEDIS = @PpaId,
                    FS_KD_PETUGAS_EDIT_MEDIS = @UserId,
                    UPDTGL = @Today,
                    UPDJAM = @NowTime,
                    UPDUSR = @UserId
                WHERE 
                    FS_KD_TRS = @TindakanId
                    AND ISNULL(FS_KD_PETUGAS_MEDIS, '') <> '';
            END
            ELSE IF EXISTS (
                SELECT 1 FROM TA_TRS_TINDAKAN2 tk
                INNER JOIN ta_detil_tarif2 dt ON tk.FS_KD_DETIL_TARIF = dt.fs_kd_detil_tarif
                WHERE tk.FS_KD_TRS = @TindakanId AND ISNULL(dt.fs_kd_sat_tugas, '') <> ''
            )
            BEGIN
                UPDATE tk
                SET 
                    tk.FS_KD_PETUGAS_MEDIS = @PpaId,
                    tk.FS_KD_PETUGAS_EDIT_MEDIS = @UserId,
                    tk.UPDTGL = @Today,
                    tk.UPDJAM = @NowTime,
                    tk.UPDUSR = @UserId
                FROM TA_TRS_TINDAKAN2 tk
                INNER JOIN ta_detil_tarif2 dt ON tk.FS_KD_DETIL_TARIF = dt.fs_kd_detil_tarif
                WHERE 
                    tk.FS_KD_TRS = @TindakanId
                    AND ISNULL(dt.fs_kd_sat_tugas, '') <> '';
            END
            ELSE
            BEGIN
                UPDATE TA_TRS_TINDAKAN2
                SET 
                    FS_KD_PETUGAS_MEDIS = @PpaId,
                    FS_KD_PETUGAS_EDIT_MEDIS = @UserId,
                    UPDTGL = @Today,
                    UPDJAM = @NowTime,
                    UPDUSR = @UserId
                WHERE 
                    FS_KD_TRS = @TindakanId;
            END

            -- 2. Update ta_trs_billing2 if matching
            UPDATE ta_trs_billing2
            SET 
                fs_kd_petugas_medis = @PpaId
            WHERE 
                fs_kd_trs = @TindakanId;
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", SafeTruncate(tindakanId, 10), SqlDbType.VarChar);
        dp.AddParam("@PpaId", SafeTruncate(ppaId, 10), SqlDbType.VarChar);
        dp.AddParam("@UserId", SafeTruncate(userId, 20), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public string GetPetugasName(string ppaId)
    {
        const string sql = "SELECT TOP 1 ISNULL(fs_nm_peg, '') FROM td_peg WHERE fs_kd_peg = @PpaId";
        var dp = new DynamicParameters();
        dp.AddParam("@PpaId", ppaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<string>(sql, dp) ?? string.Empty;
    }
}
