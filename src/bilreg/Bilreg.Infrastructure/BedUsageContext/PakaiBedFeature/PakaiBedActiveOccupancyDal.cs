using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public class PakaiBedActiveOccupancyDal : IPakaiBedActiveOccupancyDal
{
    private const string BaseSelect = """
        SELECT
            aa.PakaiBedId,
            aa.RegId,
            aa.PasienId,
            ISNULL(cc.fs_nm_pasien, '') AS PasienName,
            aa.BangsalId,
            ISNULL(bb.fs_nm_bangsal, '') AS BangsalName,
            aa.KamarId,
            ISNULL(kk.fs_nm_kamar, '') AS KamarName,
            aa.BedId,
            ISNULL(ee.fs_nm_bed, '') AS BedName,
            aa.StartedAt AS TglJamMasuk,
            aa.BillingKelasId,
            ISNULL(gg.fs_nm_kelas, '') AS BillingKelasName,
            CASE WHEN aa.PakaiBedPurpose = 4 OR aa.MotherPakaiBedId <> '' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsRoomingIn,
            aa.MotherPakaiBedId
        FROM BILRG_RnaPakaiBedAlokasi aa
            LEFT JOIN tc_mr cc ON aa.PasienId = cc.fs_mr
            LEFT JOIN ta_bed ee ON aa.BedId = ee.fs_kd_bed
            LEFT JOIN ta_kamar kk ON aa.KamarId = kk.fs_kd_kamar
            LEFT JOIN ta_bangsal bb ON aa.BangsalId = bb.fs_kd_bangsal
            LEFT JOIN ta_kelas gg ON aa.BillingKelasId = gg.fs_kd_kelas
        """;

    private readonly DatabaseOptions _opt;

    public PakaiBedActiveOccupancyDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public PakaiBedActiveOccupancyResponse? GetActiveByBed(string bedId)
    {
        const string sql = $"""
            {BaseSelect}
            WHERE aa.PakaiBedStatus = 2 AND aa.BedId = @BedId
            ORDER BY CASE WHEN aa.MotherPakaiBedId = '' THEN 0 ELSE 1 END, aa.StartedAt DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BedId", bedId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<PakaiBedActiveOccupancyDto>(sql, dp)?.ToList();
        return list?.FirstOrDefault()?.ToResponse();
    }

    public IEnumerable<PakaiBedActiveOccupancyResponse> ListActiveByWard(string bangsalId)
    {
        const string sql = $"""
            {BaseSelect}
            WHERE aa.PakaiBedStatus = 2 AND aa.BangsalId = @BangsalId
            ORDER BY aa.KamarId, aa.BedId, CASE WHEN aa.MotherPakaiBedId = '' THEN 0 ELSE 1 END, aa.StartedAt ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BangsalId", bangsalId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<PakaiBedActiveOccupancyDto>(sql, dp);
        return list?.Select(x => x.ToResponse()).ToList() ?? [];
    }
}
