using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public sealed class AdmissionQueueOperationalProjection : IAdmissionQueueOperationalProjection
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);
    private const int CommandTimeoutSeconds = 30;
    private readonly DatabaseOptions _opt;
    public AdmissionQueueOperationalProjection(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IReadOnlyList<AdmissionQueueWorklistItem> ListWorklist(
        AdmissionQueueWorklistFilter filter) => ListWorklistPage(filter).Items;

    public AdmissionQueueWorklistPage ListWorklistPage(AdmissionQueueWorklistFilter filter)
    {
        const string pageSql = """
            SELECT q.AntrianId, e.NoUrut,
                CASE WHEN q.QueuePrefixSnapshot = '' THEN NULL
                     ELSE q.QueuePrefixSnapshot + RIGHT('0000' + CONVERT(VARCHAR(4), e.NoUrut), 4) END QueueLabel,
                q.ServicePointCode ServicePointId, q.AntrianDescription ServicePointName,
                e.AntrianStatus QueueStatus, e.Priority, e.CreationReason, e.CallCount,
                c.LoketKey, c.ClaimState, e.CreatedAt,
                NULLIF(e.ServedAt, '3000-01-01') ServedAt,
                NULLIF(e.DoneAt, '3000-01-01') DoneAt,
                NULLIF(e.PasienTrackerId, '-') PasienTrackerId
            FROM BILRG_Antrian q
            INNER JOIN BILRG_AntrianEntry e ON e.AntrianId = q.AntrianId
            LEFT JOIN BILRG_AdmLoketCurrentCall c
                ON c.AntrianId = e.AntrianId AND c.NoUrut = e.NoUrut AND c.IsActive = 1
            WHERE q.AntrianDate = @BusinessDate
              AND (@ServicePointId IS NULL OR q.ServicePointCode = @ServicePointId)
              AND (@QueueStatus IS NULL OR e.AntrianStatus = @QueueStatus)
              AND (@ActiveOnly = 0 OR e.AntrianStatus IN (0, 1))
              AND (@LoketKey IS NULL OR c.LoketKey = @LoketKey)
              AND (@ServicePointIds IS NULL OR q.ServicePointCode IN (
                  SELECT value FROM STRING_SPLIT(@ServicePointIds, ',')))
            ORDER BY e.Priority DESC, e.CreatedAt, e.NoUrut, q.AntrianId
            OFFSET @Offset ROWS FETCH NEXT @FetchCount ROWS ONLY
            """;
        const string countSql = """
            SELECT COUNT_BIG(1)
            FROM BILRG_Antrian q
            INNER JOIN BILRG_AntrianEntry e ON e.AntrianId = q.AntrianId
            LEFT JOIN BILRG_AdmLoketCurrentCall c
                ON c.AntrianId = e.AntrianId AND c.NoUrut = e.NoUrut AND c.IsActive = 1
            WHERE q.AntrianDate = @BusinessDate
              AND (@ServicePointId IS NULL OR q.ServicePointCode = @ServicePointId)
              AND (@QueueStatus IS NULL OR e.AntrianStatus = @QueueStatus)
              AND (@ActiveOnly = 0 OR e.AntrianStatus IN (0, 1))
              AND (@LoketKey IS NULL OR c.LoketKey = @LoketKey)
              AND (@ServicePointIds IS NULL OR q.ServicePointCode IN (
                  SELECT value FROM STRING_SPLIT(@ServicePointIds, ',')))
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var args = new {
            BusinessDate = filter.BusinessDate.ToDateTime(TimeOnly.MinValue), filter.ServicePointId,
            filter.QueueStatus, filter.ActiveOnly, filter.LoketKey, filter.Offset,
            FetchCount = filter.Limit + 1,
            ServicePointIds = filter.ServicePointIds is null ? null : string.Join(',', filter.ServicePointIds)
        };
        var rows = conn.Query<WorklistRow>(
            new CommandDefinition(pageSql, args, commandTimeout: CommandTimeoutSeconds))
            .Select(ToItem)
            .ToList();
        var totalCount = checked((int)conn.ExecuteScalar<long>(
            new CommandDefinition(countSql, args, commandTimeout: CommandTimeoutSeconds)));
        return AdmissionQueueWorklistPaging.Create(
            rows,
            filter.Offset,
            filter.Limit,
            totalCount);
    }

    public IReadOnlyList<CurrentLoketDisplayItem> ListCurrentLoket(string? loketKey = null)
    {
        const string sql = """
            SELECT c.LoketKey, c.AntrianId, c.NoUrut,
                CASE WHEN q.QueuePrefixSnapshot = '' THEN NULL
                     ELSE q.QueuePrefixSnapshot + RIGHT('0000' + CONVERT(VARCHAR(4), c.NoUrut), 4) END QueueLabel,
                q.ServicePointCode ServicePointId, c.ClaimState DisplayState,
                c.AnnouncementVersion, c.CalledAt,
                NULLIF(c.ServiceStartedAt, '3000-01-01') ServiceStartedAt,
                c.RowVersion
            FROM BILRG_AdmLoketCurrentCall c
            INNER JOIN BILRG_Antrian q ON q.AntrianId = c.AntrianId
            WHERE c.IsActive = 1 AND (@LoketKey IS NULL OR c.LoketKey = @LoketKey)
            ORDER BY c.LoketKey
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<DisplayRow>(new CommandDefinition(
                sql,
                new { LoketKey = loketKey },
                commandTimeout: CommandTimeoutSeconds))
            .Select(x => new CurrentLoketDisplayItem(x.LoketKey, x.AntrianId, x.NoUrut,
                x.QueueLabel, x.ServicePointId, (AdmissionQueueClaimState)x.DisplayState,
                x.AnnouncementVersion, x.CalledAt, x.ServiceStartedAt, x.RowVersion)).ToList();
    }

    private static AdmissionQueueWorklistItem ToItem(WorklistRow x) => new(
        x.AntrianId, x.NoUrut, x.QueueLabel, x.ServicePointId, x.ServicePointName,
        x.QueueStatus, x.Priority, (AdmissionQueueCreationReason)x.CreationReason,
        x.CallCount, x.LoketKey, x.ClaimState is null ? null : (AdmissionQueueClaimState)x.ClaimState,
        x.CreatedAt, x.ServedAt, x.DoneAt, x.PasienTrackerId);

    private sealed record WorklistRow(string AntrianId, int NoUrut, string? QueueLabel,
        string ServicePointId, string ServicePointName, int QueueStatus, bool Priority,
        int CreationReason, int CallCount, string? LoketKey, int? ClaimState,
        DateTime CreatedAt, DateTime? ServedAt, DateTime? DoneAt, string? PasienTrackerId);
    private sealed record DisplayRow(string LoketKey, string AntrianId, int NoUrut,
        string? QueueLabel, string ServicePointId, int DisplayState, long AnnouncementVersion,
        DateTime CalledAt, DateTime? ServiceStartedAt, byte[] RowVersion);
}
