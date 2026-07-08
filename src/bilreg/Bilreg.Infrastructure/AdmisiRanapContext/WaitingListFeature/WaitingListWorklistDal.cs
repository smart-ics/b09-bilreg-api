using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;

public class WaitingListWorklistDal : IWaitingListWorklistDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly DatabaseOptions _opt;

    public WaitingListWorklistDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

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
                aa.KelasId,
                aa.KelasName,
                aa.BangsalId,
                aa.BangsalName,
                aa.CrtDate
            FROM BILRG_BedWaitingList aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                aa.VodDate = @VodDate
            """;

        if (filter.WaitingListStatus.HasValue)
            sql += " AND aa.WaitingListStatus = @WaitingListStatus";
        else
            sql += " AND aa.WaitingListStatus IN (@Waiting, @Accepted)";

        if (!string.IsNullOrWhiteSpace(filter.BangsalId))
            sql += " AND aa.BangsalId = @BangsalId";

        sql += " ORDER BY aa.Priority DESC, aa.CrtDate ASC";

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@Waiting", (int)WaitingListStatusEnum.Waiting, SqlDbType.Int);
        dp.AddParam("@Accepted", (int)WaitingListStatusEnum.Accepted, SqlDbType.Int);

        if (filter.WaitingListStatus.HasValue)
            dp.AddParam("@WaitingListStatus", filter.WaitingListStatus.Value, SqlDbType.Int);

        if (!string.IsNullOrWhiteSpace(filter.BangsalId))
            dp.AddParam("@BangsalId", filter.BangsalId.Trim(), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<WaitingListWorklistRowDto>(sql, dp);
        return rows?.Select(ToView) ?? [];
    }

    private static WaitingListWorklistView ToView(WaitingListWorklistRowDto row) =>
        new(
            row.WaitingListId,
            row.RegId,
            row.WaitingListStatus,
            row.Priority,
            row.PasienId,
            row.PasienName,
            row.Gender,
            row.KelasId,
            row.KelasName,
            row.BangsalId,
            row.BangsalName,
            row.CrtDate);

    private sealed record WaitingListWorklistRowDto(
        string WaitingListId,
        string RegId,
        int WaitingListStatus,
        int Priority,
        string PasienId,
        string PasienName,
        string Gender,
        string KelasId,
        string KelasName,
        string BangsalId,
        string BangsalName,
        DateTime CrtDate);
}
