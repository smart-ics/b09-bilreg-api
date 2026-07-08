using System.Data;
using System.Data.SqlClient;
using System.Text;
using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.OperationalWorklistFeature;

public class OperationalWorklistDal : IOperationalWorklistDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly DatabaseOptions _opt;

    public OperationalWorklistDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IEnumerable<OperationalWorklistItemView> List(OperationalWorklistFilter filter)
    {
        var branches = new List<string>();
        var includeOpname = string.IsNullOrWhiteSpace(filter.Jenis) || filter.Jenis == "opname";
        var includeReservation = string.IsNullOrWhiteSpace(filter.Jenis) || filter.Jenis == "reservation";
        var includeAdmission = string.IsNullOrWhiteSpace(filter.Jenis) || filter.Jenis == "admission";
        var includeWaitingList = string.IsNullOrWhiteSpace(filter.Jenis) || filter.Jenis == "waitingList";

        if (includeOpname)
            branches.Add(BuildOpnameBranch(filter.IncludeTerminal));

        if (includeReservation)
            branches.Add(BuildReservationBranch(filter.IncludeTerminal));

        if (includeAdmission)
            branches.Add(BuildAdmissionBranch(filter.IncludeTerminal));

        if (includeWaitingList)
            branches.Add(BuildWaitingListBranch(filter.IncludeTerminal));

        if (branches.Count == 0)
            return [];

        var sql = new StringBuilder();
        sql.AppendLine("SELECT * FROM (");
        sql.AppendLine(string.Join(Environment.NewLine + "UNION ALL" + Environment.NewLine, branches));
        sql.AppendLine(") wl WHERE 1=1");

        if (!string.IsNullOrWhiteSpace(filter.DokterId))
            sql.AppendLine(" AND wl.DokterId = @DokterId");

        if (!string.IsNullOrWhiteSpace(filter.BangsalId))
            sql.AppendLine(" AND wl.BangsalId = @BangsalId");

        if (!string.IsNullOrWhiteSpace(filter.KelasId))
            sql.AppendLine(" AND wl.KelasId = @KelasId");

        if (!string.IsNullOrWhiteSpace(filter.TipeJaminanId))
            sql.AppendLine(" AND wl.TipeJaminanId = @TipeJaminanId");

        if (filter.DateFrom.HasValue)
            sql.AppendLine(" AND wl.SortDate >= @DateFrom");

        if (filter.DateTo.HasValue)
            sql.AppendLine(" AND wl.SortDate < @DateToEnd");

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            sql.AppendLine(
                " AND (wl.PasienName LIKE @SearchPattern OR wl.PasienId LIKE @SearchPattern)");

        sql.AppendLine(" ORDER BY wl.SortDate DESC, wl.ItemId DESC");

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@OpnameCancelled", (int)OpnameRequestStatusEnum.Cancelled, SqlDbType.Int);
        dp.AddParam("@ReservationCancelled", (int)ReservationStatusEnum.Cancelled, SqlDbType.Int);
        dp.AddParam("@AdmissionCompleted", (int)AdmissionStatusEnum.Completed, SqlDbType.Int);
        dp.AddParam("@AdmissionCancelled", (int)AdmissionStatusEnum.Cancelled, SqlDbType.Int);
        dp.AddParam("@WaitingListClosed", (int)WaitingListStatusEnum.Closed, SqlDbType.Int);
        dp.AddParam("@ReservationRealized", (int)ReservationStatusEnum.Realized, SqlDbType.Int);
        dp.AddParam("@OpnameFulfilled", (int)OpnameRequestStatusEnum.Fulfilled, SqlDbType.Int);

        if (!string.IsNullOrWhiteSpace(filter.DokterId))
            dp.AddParam("@DokterId", filter.DokterId.Trim(), SqlDbType.VarChar);

        if (!string.IsNullOrWhiteSpace(filter.BangsalId))
            dp.AddParam("@BangsalId", filter.BangsalId.Trim(), SqlDbType.VarChar);

        if (!string.IsNullOrWhiteSpace(filter.KelasId))
            dp.AddParam("@KelasId", filter.KelasId.Trim(), SqlDbType.VarChar);

        if (!string.IsNullOrWhiteSpace(filter.TipeJaminanId))
            dp.AddParam("@TipeJaminanId", filter.TipeJaminanId.Trim(), SqlDbType.VarChar);

        if (filter.DateFrom.HasValue)
            dp.AddParam("@DateFrom", filter.DateFrom.Value, SqlDbType.DateTime);

        if (filter.DateTo.HasValue)
            dp.AddParam("@DateToEnd", filter.DateTo.Value.Date.AddDays(1), SqlDbType.DateTime);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            dp.AddParam("@SearchPattern", $"%{filter.SearchTerm.Trim()}%", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<OperationalWorklistRowDto>(sql.ToString(), dp);
        return rows?.Select(ToView) ?? [];
    }

    private static string BuildOpnameBranch(bool includeTerminal)
    {
        var terminalFilter = includeTerminal
            ? ""
            : " AND aa.OpnameRequestStatus NOT IN (@OpnameCancelled, @OpnameFulfilled)";

        return $"""
            SELECT
                aa.OpnameRequestId AS ItemId,
                'opname' AS Jenis,
                aa.OpnameRequestStatus AS AggregateStatus,
                aa.PasienId,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender,
                aa.DokterId,
                aa.DokterName,
                '' AS KelasId,
                '' AS KelasName,
                '' AS BangsalId,
                '' AS BangsalName,
                '' AS TipeJaminanId,
                '' AS TipeJaminanName,
                aa.CrtDate AS SortDate,
                aa.CrtDate,
                CAST(NULL AS INT) AS Priority
            FROM BILRG_AdmOpnameRequest aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.VodDate = @VodDate{terminalFilter}
            """;
    }

    private static string BuildReservationBranch(bool includeTerminal)
    {
        var terminalFilter = includeTerminal
            ? ""
            : " AND aa.ReservationStatus NOT IN (@ReservationCancelled, @ReservationRealized)";

        return $"""
            SELECT
                aa.ReservationId AS ItemId,
                'reservation' AS Jenis,
                aa.ReservationStatus AS AggregateStatus,
                aa.PasienId,
                ISNULL(aa.PasienName, '') AS PasienName,
                ISNULL(aa.Gender, '') AS Gender,
                '' AS DokterId,
                '' AS DokterName,
                aa.KelasId,
                aa.KelasName,
                aa.BangsalId,
                aa.BangsalName,
                '' AS TipeJaminanId,
                '' AS TipeJaminanName,
                aa.PlannedDate AS SortDate,
                aa.CrtDate,
                CAST(NULL AS INT) AS Priority
            FROM BILRG_AdmReservation aa
            WHERE aa.VodDate = @VodDate{terminalFilter}
            """;
    }

    private static string BuildAdmissionBranch(bool includeTerminal)
    {
        var terminalFilter = includeTerminal
            ? ""
            : " AND aa.AdmissionStatus NOT IN (@AdmissionCompleted, @AdmissionCancelled)";

        return $"""
            SELECT
                aa.RegId AS ItemId,
                'admission' AS Jenis,
                aa.AdmissionStatus AS AggregateStatus,
                aa.PasienId,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender,
                ISNULL(op.DokterId, '') AS DokterId,
                ISNULL(op.DokterName, '') AS DokterName,
                aa.KelasId,
                aa.KelasName,
                aa.BangsalId,
                aa.BangsalName,
                ISNULL(reg.fs_kd_tipe_jaminan, '') AS TipeJaminanId,
                ISNULL(tj.fs_nm_tipe_jaminan, '') AS TipeJaminanName,
                aa.AdmissionDate AS SortDate,
                aa.CrtDate,
                CAST(NULL AS INT) AS Priority
            FROM BILRG_AdmAdmission aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            LEFT JOIN BILRG_AdmOpnameRequest op
                ON aa.OpnameRequestId = op.OpnameRequestId
                AND aa.OpnameRequestId <> '-'
            LEFT JOIN ta_registrasi reg ON aa.RegId = reg.fs_kd_reg
            LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
            WHERE aa.VodDate = @VodDate{terminalFilter}
            """;
    }

    private static string BuildWaitingListBranch(bool includeTerminal)
    {
        var terminalFilter = includeTerminal
            ? ""
            : " AND aa.WaitingListStatus <> @WaitingListClosed";

        return $"""
            SELECT
                aa.WaitingListId AS ItemId,
                'waitingList' AS Jenis,
                aa.WaitingListStatus AS AggregateStatus,
                aa.PasienId,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender,
                '' AS DokterId,
                '' AS DokterName,
                aa.KelasId,
                aa.KelasName,
                aa.BangsalId,
                aa.BangsalName,
                '' AS TipeJaminanId,
                '' AS TipeJaminanName,
                aa.CrtDate AS SortDate,
                aa.CrtDate,
                aa.Priority
            FROM BILRG_BedWaitingList aa
            LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE aa.VodDate = @VodDate{terminalFilter}
            """;
    }

    private static OperationalWorklistItemView ToView(OperationalWorklistRowDto row) =>
        new(
            row.ItemId,
            row.Jenis,
            row.AggregateStatus,
            row.PasienId,
            row.PasienName,
            row.Gender,
            row.DokterId,
            row.DokterName,
            row.KelasId,
            row.KelasName,
            row.BangsalId,
            row.BangsalName,
            row.TipeJaminanId,
            row.TipeJaminanName,
            row.SortDate,
            row.CrtDate,
            row.Priority);

    private sealed record OperationalWorklistRowDto(
        string ItemId,
        string Jenis,
        int AggregateStatus,
        string PasienId,
        string PasienName,
        string Gender,
        string DokterId,
        string DokterName,
        string KelasId,
        string KelasName,
        string BangsalId,
        string BangsalName,
        string TipeJaminanId,
        string TipeJaminanName,
        DateTime SortDate,
        DateTime CrtDate,
        int? Priority);
}
