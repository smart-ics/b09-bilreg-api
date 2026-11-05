using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class BedOccupancyQuery : IBedOccupancyQuery
{
    private readonly DatabaseOptions _opt;

    public BedOccupancyQuery(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<BedOccupantItem> GetActiveOccupants(string? bangsalId, DateTime cutOffDate)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dp = new DynamicParameters();
        dp.AddParam("@BangsalId", bangsalId ?? string.Empty, SqlDbType.VarChar);
        var cutOffStr = cutOffDate.Year >= 1900 ? cutOffDate.ToString("yyyy-MM-dd") : string.Empty;
        dp.AddParam("@CutOffDate", cutOffStr, SqlDbType.VarChar);

        const string sql = """
            SELECT 
                aa.fs_kd_trs AS PakaiBedId,
                aa.fs_kd_reg AS RegId,
                ISNULL(bb.fs_mr, '') AS PasienId,
                aa.fs_kd_layanan AS BangsalId,
                ISNULL(ee.fs_kd_kamar, '') AS KamarId,
                aa.fs_kd_bed AS BedId,
                TRY_CONVERT(DATETIME, aa.fd_tgl_in + ' ' + aa.fs_jam_in) AS CheckInTime,
                ISNULL(aa.fs_kd_kelas, '') AS PhysicalClassId,
                ISNULL(NULLIF(bb.fs_kd_kelas, '000'), ISNULL(aa.fs_kd_kelas, '')) AS PayerClassId,
                CASE 
                    WHEN (ISNULL(bb.fs_reg_ibu, '') <> '' OR bb.fs_kd_jenis_inap = '3' OR aa.fn_tarif = 0) 
                         AND ISNULL(kt.fb_gabung, 0) = 1 THEN CAST(1 AS BIT) 
                    ELSE CAST(0 AS BIT) 
                END AS IsRoomingIn,
                ISNULL(mother.fs_kd_trs, '') AS MotherPakaiBedId,
                ISNULL(bb.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
                ISNULL(aa.fn_tarif, 0) AS BaseTariff,
                ISNULL(aa.fn_tarif, 0) AS PayerTariff
            FROM ta_trs_bed aa
            LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
            LEFT JOIN ta_bed ee ON aa.fs_kd_bed = ee.fs_kd_bed
            LEFT JOIN ta_kamar_tipe kt ON aa.fs_kd_kamar_tipe = kt.fs_kd_kamar_tipe
            LEFT JOIN ta_trs_bed mother ON mother.fs_kd_reg = bb.fs_reg_ibu 
                AND mother.fs_kd_bed = aa.fs_kd_bed
                AND (mother.fd_tgl_out = '' OR mother.fd_tgl_out = '3000-01-01' OR mother.fd_tgl_out IS NULL)
                AND (mother.fd_tgl_void = '' OR mother.fd_tgl_void = '3000-01-01' OR mother.fd_tgl_void IS NULL)
            WHERE (aa.fd_tgl_void = '' OR aa.fd_tgl_void = '3000-01-01' OR aa.fd_tgl_void IS NULL)
              AND (@BangsalId = '' OR aa.fs_kd_layanan = @BangsalId)
              AND (
                  (@CutOffDate = '' AND (aa.fd_tgl_out = '' OR aa.fd_tgl_out = '3000-01-01' OR aa.fd_tgl_out IS NULL))
                  OR
                  (@CutOffDate <> '' 
                   AND (aa.fd_tgl_in <= @CutOffDate OR aa.fd_tgl_in = '' OR aa.fd_tgl_in IS NULL)
                   AND (aa.fd_tgl_out = '' OR aa.fd_tgl_out = '3000-01-01' OR aa.fd_tgl_out IS NULL OR aa.fd_tgl_out >= @CutOffDate))
              )
            ORDER BY aa.fd_tgl_in ASC, aa.fs_jam_in ASC
            """;

        return conn.Read<BedOccupantItem>(sql, dp) ?? [];
    }

    public BedOccupantItem? GetOccupant(string pakaiBedId)
    {
        if (string.IsNullOrWhiteSpace(pakaiBedId))
            return null;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", pakaiBedId, SqlDbType.VarChar);

        const string sql = """
            SELECT 
                aa.fs_kd_trs AS PakaiBedId,
                aa.fs_kd_reg AS RegId,
                ISNULL(bb.fs_mr, '') AS PasienId,
                aa.fs_kd_layanan AS BangsalId,
                ISNULL(ee.fs_kd_kamar, '') AS KamarId,
                aa.fs_kd_bed AS BedId,
                TRY_CONVERT(DATETIME, aa.fd_tgl_in + ' ' + aa.fs_jam_in) AS CheckInTime,
                ISNULL(aa.fs_kd_kelas, '') AS PhysicalClassId,
                ISNULL(NULLIF(bb.fs_kd_kelas, '000'), ISNULL(aa.fs_kd_kelas, '')) AS PayerClassId,
                CASE 
                    WHEN (ISNULL(bb.fs_reg_ibu, '') <> '' OR bb.fs_kd_jenis_inap = '3' OR aa.fn_tarif = 0) 
                         AND ISNULL(kt.fb_gabung, 0) = 1 THEN CAST(1 AS BIT) 
                    ELSE CAST(0 AS BIT) 
                END AS IsRoomingIn,
                ISNULL(mother.fs_kd_trs, '') AS MotherPakaiBedId,
                ISNULL(bb.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
                ISNULL(aa.fn_tarif, 0) AS BaseTariff,
                ISNULL(aa.fn_tarif, 0) AS PayerTariff
            FROM ta_trs_bed aa
            LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
            LEFT JOIN ta_bed ee ON aa.fs_kd_bed = ee.fs_kd_bed
            LEFT JOIN ta_kamar_tipe kt ON aa.fs_kd_kamar_tipe = kt.fs_kd_kamar_tipe
            LEFT JOIN ta_trs_bed mother ON mother.fs_kd_reg = bb.fs_reg_ibu 
                AND mother.fs_kd_bed = aa.fs_kd_bed
                AND (mother.fd_tgl_out = '' OR mother.fd_tgl_out = '3000-01-01' OR mother.fd_tgl_out IS NULL)
                AND (mother.fd_tgl_void = '' OR mother.fd_tgl_void = '3000-01-01' OR mother.fd_tgl_void IS NULL)
            WHERE aa.fs_kd_trs = @PakaiBedId
            """;

        return conn.ReadSingle<BedOccupantItem>(sql, dp);
    }

    public BedOccupantItem? GetActiveOccupantByReg(string regId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        const string sql = """
            SELECT TOP 1
                aa.fs_kd_trs AS PakaiBedId,
                aa.fs_kd_reg AS RegId,
                ISNULL(bb.fs_mr, '') AS PasienId,
                aa.fs_kd_layanan AS BangsalId,
                ISNULL(ee.fs_kd_kamar, '') AS KamarId,
                aa.fs_kd_bed AS BedId,
                TRY_CONVERT(DATETIME, aa.fd_tgl_in + ' ' + aa.fs_jam_in) AS CheckInTime,
                ISNULL(aa.fs_kd_kelas, '') AS PhysicalClassId,
                ISNULL(NULLIF(bb.fs_kd_kelas, '000'), ISNULL(aa.fs_kd_kelas, '')) AS PayerClassId,
                CASE 
                    WHEN (ISNULL(bb.fs_reg_ibu, '') <> '' OR bb.fs_kd_jenis_inap = '3' OR aa.fn_tarif = 0) 
                         AND ISNULL(kt.fb_gabung, 0) = 1 THEN CAST(1 AS BIT) 
                    ELSE CAST(0 AS BIT) 
                END AS IsRoomingIn,
                ISNULL(mother.fs_kd_trs, '') AS MotherPakaiBedId,
                ISNULL(bb.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
                ISNULL(aa.fn_tarif, 0) AS BaseTariff,
                ISNULL(aa.fn_tarif, 0) AS PayerTariff
            FROM ta_trs_bed aa
            LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
            LEFT JOIN ta_bed ee ON aa.fs_kd_bed = ee.fs_kd_bed
            LEFT JOIN ta_kamar_tipe kt ON aa.fs_kd_kamar_tipe = kt.fs_kd_kamar_tipe
            LEFT JOIN ta_trs_bed mother ON mother.fs_kd_reg = bb.fs_reg_ibu 
                AND mother.fs_kd_bed = aa.fs_kd_bed
                AND (mother.fd_tgl_out = '' OR mother.fd_tgl_out = '3000-01-01' OR mother.fd_tgl_out IS NULL)
                AND (mother.fd_tgl_void = '' OR mother.fd_tgl_void = '3000-01-01' OR mother.fd_tgl_void IS NULL)
            WHERE aa.fs_kd_reg = @RegId
              AND (aa.fd_tgl_out IS NULL OR aa.fd_tgl_out = '' OR aa.fd_tgl_out = '3000-01-01')
              AND (aa.fd_tgl_void IS NULL OR aa.fd_tgl_void = '' OR aa.fd_tgl_void = '3000-01-01')
            ORDER BY 
                CASE WHEN aa.fn_tarif > 0 THEN 1 ELSE 2 END,
                aa.fd_tgl_in DESC, 
                aa.fs_jam_in DESC
            """;

        return conn.ReadSingle<BedOccupantItem>(sql, dp);
    }
}
