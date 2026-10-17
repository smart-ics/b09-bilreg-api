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

        var sql = new StringBuilder("""
            SELECT 
                aa.FS_MR AS MrId,
                ISNULL(bb.FS_NM_PASIEN, '') AS PasienName,
                ISNULL(bb.FS_ALM_PASIEN, '') AS AlamatPasien,
                ISNULL(bb.FD_TGL_LAHIR, '') AS TglLahir,
                ISNULL(bb.FS_JNS_KELAMIN, '') AS JenisKelamin,
                ISNULL(bb.FS_TLP_PASIEN, '') AS TeleponPasien,
                aa.FS_KD_MUTASI AS MutasiId,
                aa.FS_KD_TRS_BUAT AS TrsBuatId,
                ISNULL(ff.FS_KD_REG, '') AS RegId,
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
                ISNULL(ff.FN_NO_URUT, 0) AS NoAntrian,
                ISNULL(lb.FS_KD_LOKASI_BERKAS, '') AS LokasiBerkasId,
                ISNULL(lb.FS_NM_LOKASI_BERKAS, '') AS LokasiBerkasName
            FROM TA_TRS_MR_MUTASI aa
            LEFT JOIN TC_MR bb ON aa.FS_MR = bb.FS_MR
            LEFT JOIN TA_LAYANAN cc ON aa.FS_KD_LAYANAN = cc.FS_KD_LAYANAN
            LEFT JOIN TD_PEG dd ON aa.FS_KD_PETUGAS_MEDIS = dd.FS_KD_PEG
            LEFT JOIN TA_SESION_POLI ee ON aa.FS_KD_SESION_POLI = ee.FS_KD_SESION_POLI
            LEFT JOIN TA_TRS_ANTRIAN ff ON aa.FS_MR = ff.FS_MR 
                AND aa.FS_KD_LAYANAN = ff.FS_KD_LAYANAN 
                AND aa.FD_TGL_PRAKTEK = ff.FD_TGL_ANTRIAN 
                AND ff.FD_TGL_VOID = '3000-01-01'
            LEFT JOIN TC_LOKASI_BERKAS lb ON aa.FS_KD_LAYANAN = lb.FS_KD_LAYANAN
            WHERE aa.FD_TGL_VOID = '3000-01-01'
            """);

        var dp = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.TglPraktek))
        {
            sql.AppendLine("  AND aa.FD_TGL_PRAKTEK = @TglPraktek");
            dp.AddParam("@TglPraktek", filter.TglPraktek, SqlDbType.VarChar);
        }

        if (!string.IsNullOrWhiteSpace(filter.LayananId))
        {
            sql.AppendLine("  AND aa.FS_KD_LAYANAN = @LayananId");
            dp.AddParam("@LayananId", filter.LayananId, SqlDbType.VarChar);
        }

        if (!string.IsNullOrWhiteSpace(filter.SesiPoli))
        {
            sql.AppendLine("  AND aa.FS_KD_SESION_POLI = @SesiPoli");
            dp.AddParam("@SesiPoli", filter.SesiPoli, SqlDbType.VarChar);
        }

        if (filter.Status.HasValue)
        {
            sql.AppendLine("  AND aa.FN_STATUS = @Status");
            dp.AddParam("@Status", filter.Status.Value, SqlDbType.Int);
        }

        if (!string.IsNullOrWhiteSpace(filter.GedungLantai))
        {
            sql.AppendLine("  AND (lb.FS_KD_LOKASI_BERKAS LIKE @GedungLantai OR lb.FS_NM_LOKASI_BERKAS LIKE @GedungLantai)");
            dp.AddParam("@GedungLantai", $"%{filter.GedungLantai}%", SqlDbType.VarChar);
        }

        sql.AppendLine("ORDER BY aa.FN_STATUS, aa.FD_TGL_MUTASI, aa.FS_JAM_MUTASI");

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MutasiBerkasWorklistItemDto>(sql.ToString(), dp);
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
            WHERE aa.FS_MR = @MrId
            ORDER BY aa.FD_TGL_MUTASI DESC, aa.FS_JAM_MUTASI DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@MrId", mrId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MutasiBerkasHistoriItemDto>(sql, dp);
    }
}
