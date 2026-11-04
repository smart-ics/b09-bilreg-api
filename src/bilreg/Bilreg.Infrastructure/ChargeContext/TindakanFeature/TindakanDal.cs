using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface ITindakanDal :
    IInsert<TindakanDto>,
    IUpdate<TindakanDto>,
    IDelete<ITindakanKey>,
    IGetData<TindakanDto, ITindakanKey>,
    IListData<TindakanDto, IRegKey>
{
    IEnumerable<TindakanJualDto> ListTdkJual(IRegKey regKey);
}

public class TindakanDal : ITindakanDal
{
    private readonly DatabaseOptions _opt;

    public TindakanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }

    public void Insert(TindakanDto dto)
    {
        const string sql = """
            INSERT INTO TA_TRS_TINDAKAN(
                FS_KD_TRS, FS_KD_REG, FS_KD_TARIF, FS_KD_LAYANAN, FS_KD_LAYANAN_DK, FS_KD_KEGIATAN_DK,
                FS_KD_KELAS, FS_KD_TARIF_TIPE, FD_TGL_TRS, FS_JAM_TRS, FS_KD_PETUGAS,
                FD_TGL_VOID, FS_JAM_VOID, FS_KD_PETUGAS_VOID,
                FN_SUB_TOTAL, FN_DISKON, FN_TOTAL, FN_KAS, FN_BANK, FN_KARTU_KREDIT,
                FS_KD_REK_KAS, FS_KD_REK_BANK, FS_KD_REK_KARTU_KREDIT, FS_NO_KARTU_KREDIT,
                FN_SISA, FS_KD_REK_SISA, FS_KD_JENIS_NON_INAP, FS_KD_PETUGAS_KASIR, FS_KD_DOKTER_PERUJUK,
                FS_KD_TRS_DEPOSIT, fs_kd_trs_charge, fs_kd_jenis_bank, fs_kd_jenis_kartu_kredit,
                fs_kd_trs_kwitansi, fs_no_bank, fn_genkeu, FS_KD_TIPE_JAMINAN, FN_NILAI_KLAIM,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR,
                FS_CATATAN)
            VALUES(
                @FS_KD_TRS, @FS_KD_REG, @FS_KD_TARIF, @FS_KD_LAYANAN, '', '',
                @FS_KD_KELAS, @FS_KD_TARIF_TIPE, @FD_TGL_TRS, @FS_JAM_TRS, @FS_KD_PETUGAS,
                @FD_TGL_VOID, @FS_JAM_VOID, @FS_KD_PETUGAS_VOID,
                @FN_SUB_TOTAL, @FN_DISKON, @FN_TOTAL, 0, 0, 0,
                '', '', '', '',
                @FN_SISA, '', '', '', '',
                '', '', '', '',
                '', '', 0, '', 0,
                @CRTTGL, @CRTJAM, '', '', @CRTUSR,
                @UPDTGL, @UPDJAM, '', '', @UPDUSR,
                @FS_CATATAN)
            """;

        var tglTrs = dto.TindakanDate == default ? DateTime.Now : dto.TindakanDate;
        var crtDate = dto.CrtDate == default ? DateTime.Now : dto.CrtDate;
        var updDate = dto.UpdDate == default ? new DateTime(3000, 1, 1) : dto.UpdDate;
        var vodDate = dto.VodDate == default ? new DateTime(3000, 1, 1) : dto.VodDate;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_TRS", SafeTruncate(dto.TindakanId, 10), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_REG", SafeTruncate(dto.RegId, 10), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_TARIF", SafeTruncate(dto.TarifId, 10), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN", SafeTruncate(dto.LayananId, 5), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_KELAS", SafeTruncate(dto.KelasId, 3), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_TARIF_TIPE", SafeTruncate(dto.TipeTarifId, 2), SqlDbType.VarChar);
        dp.AddParam("@FD_TGL_TRS", tglTrs.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_TRS", tglTrs.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_PETUGAS", SafeTruncate(dto.CrtUser, 10), SqlDbType.VarChar);
        dp.AddParam("@FD_TGL_VOID", vodDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_VOID", vodDate.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_PETUGAS_VOID", SafeTruncate(dto.VodUser, 10), SqlDbType.VarChar);
        dp.AddParam("@FN_SUB_TOTAL", dto.Total + dto.DiskonDokter, SqlDbType.Decimal);
        dp.AddParam("@FN_DISKON", dto.DiskonDokter, SqlDbType.Decimal);
        dp.AddParam("@FN_TOTAL", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@FN_SISA", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@CRTTGL", crtDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@CRTJAM", crtDate.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", SafeTruncate(dto.CrtUser, 10), SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", updDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", updDate.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", SafeTruncate(dto.UpdUser, 10), SqlDbType.VarChar);
        dp.AddParam("@FS_CATATAN", SafeTruncate(dto.OrderTdkId, 60), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TindakanDto dto)
    {
        const string sql = """
            UPDATE 
                TA_TRS_TINDAKAN
            SET
                FS_KD_REG = @FS_KD_REG,
                FS_KD_TARIF = @FS_KD_TARIF,
                FS_KD_LAYANAN = @FS_KD_LAYANAN,
                FS_KD_KELAS = @FS_KD_KELAS,
                FS_KD_TARIF_TIPE = @FS_KD_TARIF_TIPE,
                FD_TGL_TRS = @FD_TGL_TRS,
                FS_JAM_TRS = @FS_JAM_TRS,
                FN_SUB_TOTAL = @FN_SUB_TOTAL,
                FN_DISKON = @FN_DISKON,
                FN_TOTAL = @FN_TOTAL,
                FN_SISA = @FN_SISA,
                UPDTGL = @UPDTGL,
                UPDJAM = @UPDJAM,
                UPDUSR = @UPDUSR,
                FD_TGL_VOID = @FD_TGL_VOID,
                FS_JAM_VOID = @FS_JAM_VOID,
                FS_KD_PETUGAS_VOID = @FS_KD_PETUGAS_VOID,
                FS_CATATAN = @FS_CATATAN
            WHERE
                FS_KD_TRS = @FS_KD_TRS
            """;

        var tglTrs = dto.TindakanDate == default ? DateTime.Now : dto.TindakanDate;
        var updDate = dto.UpdDate == default ? DateTime.Now : dto.UpdDate;
        var vodDate = dto.VodDate == default ? new DateTime(3000, 1, 1) : dto.VodDate;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_TRS", SafeTruncate(dto.TindakanId, 10), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_REG", SafeTruncate(dto.RegId, 10), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_TARIF", SafeTruncate(dto.TarifId, 10), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN", SafeTruncate(dto.LayananId, 5), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_KELAS", SafeTruncate(dto.KelasId, 3), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_TARIF_TIPE", SafeTruncate(dto.TipeTarifId, 2), SqlDbType.VarChar);
        dp.AddParam("@FD_TGL_TRS", tglTrs.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_TRS", tglTrs.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@FN_SUB_TOTAL", dto.Total + dto.DiskonDokter, SqlDbType.Decimal);
        dp.AddParam("@FN_DISKON", dto.DiskonDokter, SqlDbType.Decimal);
        dp.AddParam("@FN_TOTAL", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@FN_SISA", dto.Total, SqlDbType.Decimal);
        dp.AddParam("@UPDTGL", updDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", updDate.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", SafeTruncate(dto.UpdUser, 10), SqlDbType.VarChar);
        dp.AddParam("@FD_TGL_VOID", vodDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_VOID", vodDate.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@FS_KD_PETUGAS_VOID", SafeTruncate(dto.VodUser, 10), SqlDbType.VarChar);
        dp.AddParam("@FS_CATATAN", SafeTruncate(dto.OrderTdkId, 60), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);

        if (dto.VodDate != default && dto.VodDate != new DateTime(3000, 1, 1))
        {
            const string sqlVoidKomp = "UPDATE TA_TRS_TINDAKAN2 SET FB_VOID = 1 WHERE FS_KD_TRS = @FS_KD_TRS";
            conn.Execute(sqlVoidKomp, new { FS_KD_TRS = SafeTruncate(dto.TindakanId, 10) });
        }
    }

    public void Delete(ITindakanKey key)
    {
        const string sql = """
            DELETE FROM
                TA_TRS_TINDAKAN
            WHERE
                FS_KD_TRS = @FS_KD_TRS
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_TRS", key.TindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TindakanDto GetData(ITindakanKey key)
    {
        const string sql = """
            SELECT
                aa.FS_KD_TRS AS TindakanId,
                ISNULL(TRY_CONVERT(DATETIME, aa.FD_TGL_TRS + ' ' + aa.FS_JAM_TRS), '2000-01-01') AS TindakanDate,
                aa.FS_CATATAN AS OrderTdkId,
                aa.FS_KD_REG AS RegId,
                '' AS PakaiBedId,
                ISNULL(bb.FS_MR, '') AS PasienId,
                ISNULL(cc.FS_NM_PASIEN, '') AS PasienName,
                aa.FS_KD_LAYANAN AS LayananId,
                ISNULL(dd.FS_NM_LAYANAN, '') AS LayananName,
                aa.FS_KD_KELAS AS KelasId,
                ISNULL(ee.FS_NM_KELAS, '') AS KelasName,
                aa.FS_KD_TARIF_TIPE AS TipeTarifId,
                ISNULL(ff.FS_NM_TARIF_TIPE, '') AS TipeTarifName,
                aa.FS_KD_TARIF AS TarifId,
                ISNULL(gg.FS_NM_TARIF, '') AS TarifName,
                ISNULL((SELECT TOP 1 CAST(tk.FN_JML AS INT) FROM TA_TRS_TINDAKAN2 tk WHERE tk.FS_KD_TRS = aa.FS_KD_TRS), 1) AS Qty,
                aa.FN_TOTAL AS Total,
                CASE WHEN ISNULL(aa.FN_DISKON, 0) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsDiskonDokter,
                ISNULL(aa.FN_DISKON, 0) AS DiskonDokter,
                aa.CRTUSR AS CrtUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.CRTTGL + ' ' + aa.CRTJAM), '2000-01-01') AS CrtDate,
                aa.UPDUSR AS UpdUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.UPDTGL + ' ' + aa.UPDJAM), '3000-01-01') AS UpdDate,
                aa.FS_KD_PETUGAS_VOID AS VodUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.FD_TGL_VOID + ' ' + aa.FS_JAM_VOID), '3000-01-01') AS VodDate
            FROM
                TA_TRS_TINDAKAN aa
                LEFT JOIN ta_registrasi bb ON aa.FS_KD_REG = bb.FS_KD_REG
                LEFT JOIN tc_mr cc ON bb.FS_MR = cc.FS_MR
                LEFT JOIN ta_layanan dd ON aa.FS_KD_LAYANAN = dd.FS_KD_LAYANAN
                LEFT JOIN ta_kelas ee ON aa.FS_KD_KELAS = ee.FS_KD_KELAS
                LEFT JOIN ta_tarif_tipe ff ON aa.FS_KD_TARIF_TIPE = ff.FS_KD_TARIF_TIPE
                LEFT JOIN ta_tarif gg ON aa.FS_KD_TARIF = gg.FS_KD_TARIF
            WHERE
                aa.FS_KD_TRS = @TindakanId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", key.TindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TindakanDto>(sql, dp);
    }

    public IEnumerable<TindakanDto> ListData(IRegKey filter)
    {
        const string sql = """
            SELECT
                aa.FS_KD_TRS AS TindakanId,
                ISNULL(TRY_CONVERT(DATETIME, aa.FD_TGL_TRS + ' ' + aa.FS_JAM_TRS), '2000-01-01') AS TindakanDate,
                aa.FS_CATATAN AS OrderTdkId,
                aa.FS_KD_REG AS RegId,
                '' AS PakaiBedId,
                ISNULL(bb.FS_MR, '') AS PasienId,
                ISNULL(cc.FS_NM_PASIEN, '') AS PasienName,
                aa.FS_KD_LAYANAN AS LayananId,
                ISNULL(dd.FS_NM_LAYANAN, '') AS LayananName,
                aa.FS_KD_KELAS AS KelasId,
                ISNULL(ee.FS_NM_KELAS, '') AS KelasName,
                aa.FS_KD_TARIF_TIPE AS TipeTarifId,
                ISNULL(ff.FS_NM_TARIF_TIPE, '') AS TipeTarifName,
                aa.FS_KD_TARIF AS TarifId,
                ISNULL(gg.FS_NM_TARIF, '') AS TarifName,
                ISNULL((SELECT TOP 1 CAST(tk.FN_JML AS INT) FROM TA_TRS_TINDAKAN2 tk WHERE tk.FS_KD_TRS = aa.FS_KD_TRS), 1) AS Qty,
                aa.FN_TOTAL AS Total,
                CASE WHEN ISNULL(aa.FN_DISKON, 0) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsDiskonDokter,
                ISNULL(aa.FN_DISKON, 0) AS DiskonDokter,
                aa.CRTUSR AS CrtUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.CRTTGL + ' ' + aa.CRTJAM), '2000-01-01') AS CrtDate,
                aa.UPDUSR AS UpdUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.UPDTGL + ' ' + aa.UPDJAM), '3000-01-01') AS UpdDate,
                aa.FS_KD_PETUGAS_VOID AS VodUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.FD_TGL_VOID + ' ' + aa.FS_JAM_VOID), '3000-01-01') AS VodDate
            FROM
                TA_TRS_TINDAKAN aa
                LEFT JOIN ta_registrasi bb ON aa.FS_KD_REG = bb.FS_KD_REG
                LEFT JOIN tc_mr cc ON bb.FS_MR = cc.FS_MR
                LEFT JOIN ta_layanan dd ON aa.FS_KD_LAYANAN = dd.FS_KD_LAYANAN
                LEFT JOIN ta_kelas ee ON aa.FS_KD_KELAS = ee.FS_KD_KELAS
                LEFT JOIN ta_tarif_tipe ff ON aa.FS_KD_TARIF_TIPE = ff.FS_KD_TARIF_TIPE
                LEFT JOIN ta_tarif gg ON aa.FS_KD_TARIF = gg.FS_KD_TARIF
            WHERE
                aa.FS_KD_REG = @RegId
                AND (aa.FD_TGL_VOID = @VodDate OR aa.FD_TGL_VOID = '' OR aa.FD_TGL_VOID IS NULL)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", "3000-01-01", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanDto>(sql, dp);
    }

    public IEnumerable<TindakanJualDto> ListTdkJual(IRegKey regKey)
    {
        const string sql = """
            SELECT
                aa.FS_KD_TRS AS TransaksiId,
                ISNULL(TRY_CONVERT(DATETIME, aa.FD_TGL_TRS + ' ' + aa.FS_JAM_TRS), '2000-01-01') AS TransaksiDate, 
                aa.FS_CATATAN AS OrderTransaksiId,
                aa.FS_KD_REG AS RegId,
                ISNULL(rg.FS_MR, '') AS PasienId,
                ISNULL(pmr.FS_NM_PASIEN, '') AS PasienName, 
                aa.FS_KD_LAYANAN AS LayananId,
                ISNULL(ly.FS_NM_LAYANAN, '') AS LayananName, 
                aa.FS_KD_TARIF AS DiskripsiId,
                ISNULL(tr.FS_NM_TARIF, '') AS DiskripsiName, 
                ISNULL((SELECT TOP 1 CAST(tk.FN_JML AS INT) FROM TA_TRS_TINDAKAN2 tk WHERE tk.FS_KD_TRS = aa.FS_KD_TRS), 1) AS Qty,
                aa.FN_TOTAL AS Total,
                'TINDAKAN' AS Tipe,
                ISNULL(
                    NULLIF((SELECT TOP 1 p.FS_NM_PEG FROM TA_TRS_TINDAKAN2 tk LEFT JOIN td_peg p ON tk.FS_KD_PETUGAS_MEDIS = p.FS_KD_PEG WHERE tk.FS_KD_TRS = aa.FS_KD_TRS AND ISNULL(tk.FS_KD_PETUGAS_MEDIS, '') <> '' ORDER BY tk.FS_KD_DETIL_TARIF), ''),
                    ISNULL(peg.fs_nm_peg, aa.CRTUSR)
                ) AS CrtUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.CRTTGL + ' ' + aa.CRTJAM), '2000-01-01') AS CrtDate,
                aa.UPDUSR AS UpdUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.UPDTGL + ' ' + aa.UPDJAM), '3000-01-01') AS UpdDate,
                aa.FS_KD_PETUGAS_VOID AS VodUser,
                ISNULL(TRY_CONVERT(DATETIME, aa.FD_TGL_VOID + ' ' + aa.FS_JAM_VOID), '3000-01-01') AS VodDate,
                '' AS PakaiBedId
            FROM
                TA_TRS_TINDAKAN aa
                LEFT JOIN td_peg peg ON aa.CRTUSR = peg.fs_kd_peg
                LEFT JOIN ta_registrasi rg ON aa.FS_KD_REG = rg.FS_KD_REG
                LEFT JOIN tc_mr pmr ON rg.FS_MR = pmr.FS_MR
                LEFT JOIN ta_layanan ly ON aa.FS_KD_LAYANAN = ly.FS_KD_LAYANAN
                LEFT JOIN ta_tarif tr ON aa.FS_KD_TARIF = tr.FS_KD_TARIF
            WHERE
                aa.FS_KD_REG = @RegId
                AND (aa.FD_TGL_VOID = @VodDate OR aa.FD_TGL_VOID = '' OR aa.FD_TGL_VOID IS NULL)

            UNION ALL

            SELECT
                bb.BhpRanapId AS TransaksiId,
                bb.CrtDate AS TransaksiDate,
                '' AS OrderTransaksiId,
                bb.RegId,
                ISNULL(rg.FS_MR, '') AS PasienId,
                ISNULL(pmr.FS_NM_PASIEN, '') AS PasienName,
                bb.LayananId,
                ISNULL(ly.FS_NM_LAYANAN, '') AS LayananName,
                bb.BhpItemId AS DiskripsiId,
                bb.BhpItemName AS DiskripsiName,
                bb.Qty,
                (bb.Qty * bb.Price) AS Total,
                'BHP' AS Tipe,
                ISNULL(peg.fs_nm_peg, bb.CrtUser) AS CrtUser,
                bb.CrtDate,
                '' AS UpdUser,
                '3000-01-01' AS UpdDate,
                bb.VodUser,
                bb.VodDate,
                bb.PakaiBedId
            FROM
                BILRG_BhpRanap bb
                LEFT JOIN td_peg peg ON bb.CrtUser = peg.fs_kd_peg
                LEFT JOIN ta_registrasi rg ON bb.RegId = rg.FS_KD_REG
                LEFT JOIN tc_mr pmr ON rg.FS_MR = pmr.FS_MR
                LEFT JOIN ta_layanan ly ON bb.LayananId = ly.FS_KD_LAYANAN
            WHERE
                bb.RegId = @RegId
                AND bb.VodDate = @VodDate
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regKey.RegId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", "3000-01-01", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanJualDto>(sql, dp);
    }
}
