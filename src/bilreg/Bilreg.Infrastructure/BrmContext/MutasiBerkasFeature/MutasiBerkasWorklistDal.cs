using System.Data;
using System.Data.SqlClient;
using System.Text;
using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasWorklistDal : IMutasiBerkasWorklistDal
{
    private readonly DatabaseOptions _opt;

    public MutasiBerkasWorklistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<MutasiBerkasWorklistItemDto> QueryWorklist(MutasiBerkasWorklistFilter filter)
    {
        filter ??= new MutasiBerkasWorklistFilter();

        var tglPraktek = string.IsNullOrWhiteSpace(filter.TglPraktek)
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : filter.TglPraktek;

        var dp = new DynamicParameters();
        dp.AddParam("@TglPraktek", tglPraktek, SqlDbType.VarChar);

        var sql = new StringBuilder("""
            SELECT 
                LTRIM(RTRIM(ISNULL(worklist.MrId, ''))) AS MrId,
                worklist.PasienName,
                worklist.AlamatPasien,
                worklist.TglLahir,
                worklist.JenisKelamin,
                worklist.TeleponPasien,
                LTRIM(RTRIM(ISNULL(worklist.MutasiId, ''))) AS MutasiId,
                LTRIM(RTRIM(ISNULL(worklist.TrsBuatId, ''))) AS TrsBuatId,
                LTRIM(RTRIM(ISNULL(worklist.RegId, ''))) AS RegId,
                worklist.TglMutasi,
                worklist.JamMutasi,
                worklist.DokterId,
                worklist.DokterName,
                worklist.LayananId,
                worklist.LayananName,
                worklist.TglPraktek,
                worklist.JamPraktek,
                worklist.SesiPoliId,
                worklist.SesiPoliName,
                worklist.Status,
                worklist.FrekuensiCetak,
                worklist.NoAntrian,
                worklist.LokasiBerkasId,
                worklist.LokasiBerkasName
            FROM (
                SELECT 
                    ff.FS_MR AS MrId,
                    ISNULL(bb.FS_NM_PASIEN, '') AS PasienName,
                    ISNULL(bb.FS_ALM_PASIEN, '') AS AlamatPasien,
                    ISNULL(bb.FD_TGL_LAHIR, '') AS TglLahir,
                    ISNULL(bb.FS_JNS_KELAMIN, '') AS JenisKelamin,
                    ISNULL(bb.FS_TLP_PASIEN, '') AS TeleponPasien,
                    ISNULL(aa.FS_KD_MUTASI, '') AS MutasiId,
                    CASE 
                        WHEN aa.FS_KD_TRS_BUAT IS NOT NULL AND aa.FS_KD_TRS_BUAT <> '' THEN aa.FS_KD_TRS_BUAT 
                        ELSE ISNULL(ff.FS_KD_REG, '') 
                    END AS TrsBuatId,
                    ISNULL(ff.FS_KD_REG, '') AS RegId,
                    ISNULL(aa.FD_TGL_MUTASI, '') AS TglMutasi,
                    ISNULL(aa.FS_JAM_MUTASI, '') AS JamMutasi,
                    CASE 
                        WHEN aa.FS_KD_PETUGAS_MEDIS IS NOT NULL AND aa.FS_KD_PETUGAS_MEDIS <> '' THEN aa.FS_KD_PETUGAS_MEDIS 
                        ELSE ISNULL(reg.FS_KD_MEDIS, '') 
                    END AS DokterId,
                    ISNULL(dd.FS_NM_PEG, '') AS DokterName,
                    ff.FS_KD_LAYANAN AS LayananId,
                    ISNULL(cc.FS_NM_LAYANAN, '') AS LayananName,
                    ff.FD_TGL_ANTRIAN AS TglPraktek,
                    CASE
                        WHEN aa.FS_JAM_PRAKTEK IS NOT NULL AND aa.FS_JAM_PRAKTEK <> '' THEN aa.FS_JAM_PRAKTEK
                        ELSE ISNULL(reg.FS_JAM_MASUK, '')
                    END AS JamPraktek,
                    ISNULL(aa.FS_KD_SESION_POLI, '') AS SesiPoliId,
                    ISNULL(ee.FS_NM_SESION_POLI, '') AS SesiPoliName,
                    ISNULL(aa.FN_STATUS, 0) AS Status,
                    ISNULL(aa.FN_CETAK, 0) AS FrekuensiCetak,
                    ISNULL(ff.FN_NO_URUT, 0) AS NoAntrian,
                    ISNULL(lb.FS_KD_LOKASI_BERKAS, '') AS LokasiBerkasId,
                    ISNULL(lb.FS_NM_LOKASI_BERKAS, '') AS LokasiBerkasName
                FROM TA_TRS_ANTRIAN ff
                LEFT JOIN TA_REGISTRASI reg ON ff.FS_KD_REG = reg.FS_KD_REG
                LEFT JOIN TA_TRS_MR_MUTASI aa ON ff.FS_MR = aa.FS_MR
                    AND ff.FS_KD_LAYANAN = aa.FS_KD_LAYANAN
                    AND ff.FD_TGL_ANTRIAN = aa.FD_TGL_PRAKTEK
                    AND aa.FD_TGL_VOID = '3000-01-01'
                LEFT JOIN TC_MR bb ON ff.FS_MR = bb.FS_MR
                LEFT JOIN TA_LAYANAN cc ON ff.FS_KD_LAYANAN = cc.FS_KD_LAYANAN
                LEFT JOIN TD_PEG dd ON (CASE WHEN aa.FS_KD_PETUGAS_MEDIS IS NOT NULL AND aa.FS_KD_PETUGAS_MEDIS <> '' THEN aa.FS_KD_PETUGAS_MEDIS ELSE reg.FS_KD_MEDIS END) = dd.FS_KD_PEG
                LEFT JOIN TA_SESION_POLI ee ON aa.FS_KD_SESION_POLI = ee.FS_KD_SESION_POLI
                LEFT JOIN TC_LOKASI_BERKAS lb ON ff.FS_KD_LAYANAN = lb.FS_KD_LAYANAN
                WHERE ff.FD_TGL_VOID = '3000-01-01'
                  AND ff.FD_TGL_ANTRIAN = @TglPraktek
                  AND (
                      LTRIM(RTRIM(ISNULL(ff.FS_MR, ''))) <> '' 
                      OR (aa.FS_KD_MUTASI IS NOT NULL AND LTRIM(RTRIM(aa.FS_KD_MUTASI)) <> '')
                  )

                UNION ALL

                SELECT 
                    aa.FS_MR AS MrId,
                    ISNULL(bb.FS_NM_PASIEN, '') AS PasienName,
                    ISNULL(bb.FS_ALM_PASIEN, '') AS AlamatPasien,
                    ISNULL(bb.FD_TGL_LAHIR, '') AS TglLahir,
                    ISNULL(bb.FS_JNS_KELAMIN, '') AS JenisKelamin,
                    ISNULL(bb.FS_TLP_PASIEN, '') AS TeleponPasien,
                    aa.FS_KD_MUTASI AS MutasiId,
                    aa.FS_KD_TRS_BUAT AS TrsBuatId,
                    '' AS RegId,
                    aa.FD_TGL_MUTASI AS TglMutasi,
                    aa.FS_JAM_MUTASI AS JamMutasi,
                    aa.FS_KD_PETUGAS_MEDIS AS DokterId,
                    ISNULL(dd.FS_NM_PEG, '') AS DokterName,
                    aa.FS_KD_LAYANAN AS LayananId,
                    ISNULL(cc.FS_NM_LAYANAN, '') AS LayananName,
                    aa.FD_TGL_PRAKTEK AS TglPraktek,
                    aa.FS_JAM_PRAKTEK AS JamPraktek,
                    aa.FS_KD_SESION_POLI AS SesiPoliId,
                    ISNULL(ee.FS_NM_SESION_POLI, '') AS SesiPoliName,
                    aa.FN_STATUS AS Status,
                    aa.FN_CETAK AS FrekuensiCetak,
                    0 AS NoAntrian,
                    ISNULL(lb.FS_KD_LOKASI_BERKAS, '') AS LokasiBerkasId,
                    ISNULL(lb.FS_NM_LOKASI_BERKAS, '') AS LokasiBerkasName
                FROM TA_TRS_MR_MUTASI aa
                LEFT JOIN TC_MR bb ON aa.FS_MR = bb.FS_MR
                LEFT JOIN TA_LAYANAN cc ON aa.FS_KD_LAYANAN = cc.FS_KD_LAYANAN
                LEFT JOIN TD_PEG dd ON aa.FS_KD_PETUGAS_MEDIS = dd.FS_KD_PEG
                LEFT JOIN TA_SESION_POLI ee ON aa.FS_KD_SESION_POLI = ee.FS_KD_SESION_POLI
                LEFT JOIN TC_LOKASI_BERKAS lb ON aa.FS_KD_LAYANAN = lb.FS_KD_LAYANAN
                WHERE aa.FD_TGL_VOID = '3000-01-01'
                  AND aa.FD_TGL_PRAKTEK = @TglPraktek
                  AND (
                      LTRIM(RTRIM(ISNULL(aa.FS_MR, ''))) <> '' 
                      OR LTRIM(RTRIM(ISNULL(aa.FS_KD_MUTASI, ''))) <> ''
                  )
                  AND NOT EXISTS (
                      SELECT 1 FROM TA_TRS_ANTRIAN sub
                      WHERE sub.FS_MR = aa.FS_MR
                        AND sub.FS_KD_LAYANAN = aa.FS_KD_LAYANAN
                        AND sub.FD_TGL_ANTRIAN = aa.FD_TGL_PRAKTEK
                        AND sub.FD_TGL_VOID = '3000-01-01'
                  )
            ) worklist
            WHERE 1 = 1
              AND (
                  LTRIM(RTRIM(ISNULL(worklist.MrId, ''))) <> '' 
                  OR LTRIM(RTRIM(ISNULL(worklist.MutasiId, ''))) <> ''
              )
            """);

        if (!string.IsNullOrWhiteSpace(filter.LayananId))
        {
            sql.AppendLine("  AND worklist.LayananId = @LayananId");
            dp.AddParam("@LayananId", filter.LayananId, SqlDbType.VarChar);
        }

        if (!string.IsNullOrWhiteSpace(filter.SesiPoli))
        {
            sql.AppendLine("  AND worklist.SesiPoliId = @SesiPoli");
            dp.AddParam("@SesiPoli", filter.SesiPoli, SqlDbType.VarChar);
        }

        if (filter.Status.HasValue)
        {
            sql.AppendLine("  AND worklist.Status = @Status");
            dp.AddParam("@Status", filter.Status.Value, SqlDbType.Int);
        }

        if (!string.IsNullOrWhiteSpace(filter.GedungLantai))
        {
            sql.AppendLine("  AND (worklist.LokasiBerkasId LIKE @GedungLantai OR worklist.LokasiBerkasName LIKE @GedungLantai)");
            dp.AddParam("@GedungLantai", $"%{filter.GedungLantai}%", SqlDbType.VarChar);
        }

        sql.AppendLine("ORDER BY worklist.Status, worklist.NoAntrian, worklist.JamPraktek, worklist.JamMutasi");

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MutasiBerkasWorklistItemDto>(sql.ToString(), dp) ?? [];
    }

    public IEnumerable<MutasiBerkasHistoriItemDto> QueryHistori(string mrId)
    {
        if (string.IsNullOrWhiteSpace(mrId))
            return [];

        const string sql = """
            SELECT 
                aa.FS_KD_MUTASI AS MutasiId,
                aa.FS_KD_TRS_BUAT AS TrsBuatId,
                aa.FS_MR AS MrId,
                aa.FD_TGL_MUTASI AS TglMutasi,
                aa.FS_JAM_MUTASI AS JamMutasi,
                aa.FS_KD_PETUGAS_MEDIS AS DokterId,
                ISNULL(cc.FS_NM_PEG, '') AS DokterName,
                aa.FS_KD_LAYANAN AS LayananId,
                ISNULL(bb.FS_NM_LAYANAN, '') AS LayananName,
                aa.FD_TGL_PRAKTEK AS TglPraktek,
                aa.FS_JAM_PRAKTEK AS JamPraktek,
                aa.FS_KD_SESION_POLI AS SesiPoliId,
                aa.FN_STATUS AS Status,
                aa.FS_KD_PETUGAS AS PetugasId,
                aa.FN_CETAK AS FrekuensiCetak,
                aa.FS_KD_PETUGAS_CETAK AS PetugasCetak,
                aa.fd_tgl_cetak AS TglCetak,
                aa.fs_jam_cetak AS JamCetak,
                CASE WHEN aa.fd_tgl_void <> '3000-01-01' THEN 1 ELSE 0 END AS IsVoid,
                aa.fd_tgl_void AS TglVoid,
                aa.fs_jam_void AS JamVoid,
                aa.fs_kd_petugas_void AS PetugasVoid
            FROM TA_TRS_MR_MUTASI aa
            LEFT JOIN TA_LAYANAN bb ON aa.FS_KD_LAYANAN = bb.FS_KD_LAYANAN
            LEFT JOIN TD_PEG cc ON aa.FS_KD_PETUGAS_MEDIS = cc.FS_KD_PEG
            WHERE REPLACE(aa.FS_MR, '-', '') = REPLACE(@MrId, '-', '')
            ORDER BY aa.FD_TGL_MUTASI DESC, aa.FS_JAM_MUTASI DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@MrId", mrId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MutasiBerkasHistoriItemDto>(sql, dp) ?? [];
    }
}
