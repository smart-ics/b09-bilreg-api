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

        const string modernSql = """
            SELECT 
                aa.PakaiBedId,
                aa.RegId,
                aa.PasienId,
                aa.BangsalId,
                aa.KamarId,
                aa.BedId,
                aa.StartedAt AS CheckInTime,
                ISNULL(ee.fs_kd_kelas, '') AS PhysicalClassId,
                ISNULL(bb.fs_kd_kelas, ISNULL(ee.fs_kd_kelas, '')) AS PayerClassId,
                CASE WHEN aa.PakaiBedPurpose = 4 THEN 1 ELSE 0 END AS IsRoomingIn,
                ISNULL(bb.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
                ISNULL(ee.fn_tarif, 0) AS BaseTariff,
                ISNULL(ee.fn_tarif, 0) AS PayerTariff
            FROM BILRG_RnaPakaiBedAlokasi aa
            LEFT JOIN ta_registrasi bb ON aa.RegId = bb.fs_kd_reg
            LEFT JOIN ta_bed ee ON aa.BedId = ee.fs_kd_bed
            WHERE aa.PakaiBedStatus = 2
              AND (@BangsalId = '' OR aa.BangsalId = @BangsalId)
            """;

        try
        {
            var results = conn.Read<BedOccupantItem>(modernSql, dp)?.ToList();
            if (results != null && results.Count > 0)
                return results;
        }
        catch
        {
            // Fall back to legacy table
        }

        const string legacySql = """
            SELECT 
                aa.fs_kd_trs AS PakaiBedId,
                aa.fs_kd_reg AS RegId,
                ISNULL(bb.fs_mr, '') AS PasienId,
                aa.fs_kd_layanan AS BangsalId,
                ISNULL(ee.fs_kd_kamar, '') AS KamarId,
                aa.fs_kd_bed AS BedId,
                TRY_CONVERT(DATETIME, aa.fd_tgl_in + ' ' + aa.fs_jam_in) AS CheckInTime,
                ISNULL(aa.fs_kd_kelas, '') AS PhysicalClassId,
                ISNULL(bb.fs_kd_kelas, ISNULL(aa.fs_kd_kelas, '')) AS PayerClassId,
                0 AS IsRoomingIn,
                ISNULL(bb.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
                ISNULL(aa.fn_tarif, 0) AS BaseTariff,
                ISNULL(aa.fn_tarif, 0) AS PayerTariff
            FROM ta_trs_bed aa
            LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
            LEFT JOIN ta_bed ee ON aa.fs_kd_bed = ee.fs_kd_bed
            WHERE (aa.fd_tgl_out = '' OR aa.fd_tgl_out = '3000-01-01' OR aa.fd_tgl_out IS NULL)
              AND (aa.fd_tgl_void = '' OR aa.fd_tgl_void = '3000-01-01' OR aa.fd_tgl_void IS NULL)
              AND (@BangsalId = '' OR aa.fs_kd_layanan = @BangsalId)
            """;

        return conn.Read<BedOccupantItem>(legacySql, dp) ?? [];
    }

    public BedOccupantItem? GetOccupant(string pakaiBedId)
    {
        if (string.IsNullOrWhiteSpace(pakaiBedId))
            return null;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", pakaiBedId, SqlDbType.VarChar);

        const string modernSql = """
            SELECT 
                aa.PakaiBedId,
                aa.RegId,
                aa.PasienId,
                aa.BangsalId,
                aa.KamarId,
                aa.BedId,
                aa.StartedAt AS CheckInTime,
                ISNULL(ee.fs_kd_kelas, '') AS PhysicalClassId,
                ISNULL(bb.fs_kd_kelas, ISNULL(ee.fs_kd_kelas, '')) AS PayerClassId,
                CASE WHEN aa.PakaiBedPurpose = 4 THEN 1 ELSE 0 END AS IsRoomingIn,
                ISNULL(bb.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
                ISNULL(ee.fn_tarif, 0) AS BaseTariff,
                ISNULL(ee.fn_tarif, 0) AS PayerTariff
            FROM BILRG_RnaPakaiBedAlokasi aa
            LEFT JOIN ta_registrasi bb ON aa.RegId = bb.fs_kd_reg
            LEFT JOIN ta_bed ee ON aa.BedId = ee.fs_kd_bed
            WHERE aa.PakaiBedId = @PakaiBedId
            """;

        try
        {
            var item = conn.ReadSingle<BedOccupantItem>(modernSql, dp);
            if (item != null)
                return item;
        }
        catch
        {
            // Fall back to legacy table
        }

        const string legacySql = """
            SELECT 
                aa.fs_kd_trs AS PakaiBedId,
                aa.fs_kd_reg AS RegId,
                ISNULL(bb.fs_mr, '') AS PasienId,
                aa.fs_kd_layanan AS BangsalId,
                ISNULL(ee.fs_kd_kamar, '') AS KamarId,
                aa.fs_kd_bed AS BedId,
                TRY_CONVERT(DATETIME, aa.fd_tgl_in + ' ' + aa.fs_jam_in) AS CheckInTime,
                ISNULL(aa.fs_kd_kelas, '') AS PhysicalClassId,
                ISNULL(bb.fs_kd_kelas, ISNULL(aa.fs_kd_kelas, '')) AS PayerClassId,
                0 AS IsRoomingIn,
                ISNULL(bb.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
                ISNULL(aa.fn_tarif, 0) AS BaseTariff,
                ISNULL(aa.fn_tarif, 0) AS PayerTariff
            FROM ta_trs_bed aa
            LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
            LEFT JOIN ta_bed ee ON aa.fs_kd_bed = ee.fs_kd_bed
            WHERE aa.fs_kd_trs = @PakaiBedId
            """;

        return conn.ReadSingle<BedOccupantItem>(legacySql, dp);
    }
}
