using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WaitingListFeature;

public class WaitingListWorklistDal : IWaitingListWorklistDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);
    private readonly DatabaseOptions _opt;

    public WaitingListWorklistDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IEnumerable<WaitingListWorklistView> ListActiveWorklist(string bangsalId)
    {
        const string sql = """
            SELECT
                aa.WaitingListId,
                aa.RegId,
                aa.WaitingListStatus,
                aa.Priority,
                aa.PasienId,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender,
                ISNULL(CONVERT(VARCHAR(10), bb.fd_tgl_lahir, 120), '3000-01-01') AS TglLahir,
                aa.KelasId,
                aa.KelasName,
                aa.BangsalId,
                aa.BangsalName,
                aa.ArrivalSourceType,
                aa.ReferringUnitId,
                aa.ReferringUnitName,
                aa.ReferringDoctorId,
                aa.ReferringDoctorName,
                aa.ClinicalUrgency,
                aa.SpecialRequirements,
                aa.PlannedBedId,
                aa.PlannedBedName,
                aa.PreviousWaitListId,
                aa.CrtUser,
                aa.CrtDate
            FROM BILRG_BedWaitingList aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE BangsalId = @BangsalId AND WaitingListStatus IN (0, 1)
            ORDER BY ClinicalUrgency DESC, CrtDate ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BangsalId", bangsalId?.Trim() ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<WaitingListWorklistView>(sql, dp);
        return rows ?? [];
    }

    public IEnumerable<WaitingListWorklistView> List(WaitingListWorklistFilter filter)
    {
        var sql = """
            SELECT
                aa.WaitingListId,
                aa.RegId,
                aa.WaitingListStatus,
                aa.Priority,
                aa.PasienId,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender,
                ISNULL(CONVERT(VARCHAR(10), bb.fd_tgl_lahir, 120), '3000-01-01') AS TglLahir,
                aa.KelasId,
                aa.KelasName,
                aa.BangsalId,
                aa.BangsalName,
                aa.ArrivalSourceType,
                aa.ReferringUnitId,
                aa.ReferringUnitName,
                aa.ReferringDoctorId,
                aa.ReferringDoctorName,
                aa.ClinicalUrgency,
                aa.SpecialRequirements,
                aa.PlannedBedId,
                aa.PlannedBedName,
                aa.PreviousWaitListId,
                aa.CrtUser,
                aa.CrtDate
            FROM BILRG_BedWaitingList aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.VodDate = @VodDate
            """;

        if (filter.WaitingListStatus.HasValue)
            sql += " AND aa.WaitingListStatus = @WaitingListStatus";
        else
            sql += " AND aa.WaitingListStatus IN (@Waiting, @Reserved, @Rerouted, @Rejected)";

        if (!string.IsNullOrWhiteSpace(filter.BangsalId))
            sql += " AND aa.BangsalId = @BangsalId";

        sql += " ORDER BY aa.Priority DESC, aa.CrtDate ASC";

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@Waiting", (int)WaitingListStatusEnum.Waiting, SqlDbType.Int);
        dp.AddParam("@Reserved", (int)WaitingListStatusEnum.Reserved, SqlDbType.Int);
        dp.AddParam("@Rerouted", (int)WaitingListStatusEnum.Rerouted, SqlDbType.Int);
        dp.AddParam("@Rejected", (int)WaitingListStatusEnum.Rejected, SqlDbType.Int);

        if (filter.WaitingListStatus.HasValue)
            dp.AddParam("@WaitingListStatus", filter.WaitingListStatus.Value, SqlDbType.Int);

        if (!string.IsNullOrWhiteSpace(filter.BangsalId))
            dp.AddParam("@BangsalId", filter.BangsalId.Trim(), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<WaitingListWorklistView>(sql, dp);
        return rows ?? [];
    }
}
