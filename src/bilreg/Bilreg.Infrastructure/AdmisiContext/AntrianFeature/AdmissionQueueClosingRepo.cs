using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

/// <summary>Owns the single serializable database transaction used for supervisor queue closing.</summary>
public sealed class AdmissionQueueClosingRepo : IAdmissionQueueClosingRepo
{
    private readonly DatabaseOptions _options;
    public AdmissionQueueClosingRepo(IOptions<DatabaseOptions> options) => _options = options.Value;
    private SqlConnection Open() => new(ConnStringHelper.Get(_options));

    public IReadOnlyList<AdmissionQueueClosingPreviewItem> ListWaiting(DateOnly businessDate, string servicePointId)
    {
        using var connection = Open();
        return connection.Query<Row>(SelectSql, new { BusinessDate = businessDate.ToDateTime(TimeOnly.MinValue), ServicePointId = servicePointId })
            .Select(ToPreview).ToList();
    }

    public AdmissionQueueCloseResponse Close(AdmissionQueueCloseCmd command, DateOnly businessDate, DateTime closedAt)
    {
        using var connection = Open();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var hasInService = connection.ExecuteScalar<bool>("""
                SELECT CAST(IIF(EXISTS(
                    SELECT 1 FROM BILRG_Antrian q WITH (UPDLOCK,HOLDLOCK)
                    INNER JOIN BILRG_AntrianEntry e WITH (UPDLOCK,HOLDLOCK) ON e.AntrianId=q.AntrianId
                    INNER JOIN BILRG_AdmLoketCurrentCall c WITH (UPDLOCK,HOLDLOCK) ON c.AntrianId=e.AntrianId AND c.NoUrut=e.NoUrut AND c.IsActive=1
                    WHERE q.AntrianDate=@BusinessDate AND q.ServicePointCode=@ServicePointId AND c.ClaimState=2),1,0) AS BIT);
                """, new { BusinessDate = businessDate.ToDateTime(TimeOnly.MinValue), command.ServicePointId }, transaction);
            if (hasInService) throw new InvalidOperationException("Queue Closing is blocked by an InService entry.");
            var rows = connection.Query<Row>(LockedSelectSql, new { BusinessDate = businessDate.ToDateTime(TimeOnly.MinValue), command.ServicePointId }, transaction).ToList();
            var actual = rows.Select(x => Key(x.AntrianId, x.NoUrut)).ToHashSet(StringComparer.Ordinal);
            var submitted = command.Decisions.Select(x => Key(x.AntrianId, x.NoUrut)).ToHashSet(StringComparer.Ordinal);
            if (!actual.SetEquals(submitted)) throw new AdmissionQueueConcurrencyException("Remaining queue entries changed; refresh the closing review.");

            foreach (var row in rows)
            {
                var decision = command.Decisions.Single(x => x.AntrianId == row.AntrianId && x.NoUrut == row.NoUrut);
                if (row.ClaimState == (int)AdmissionQueueClaimState.Outstanding &&
                    (decision.ExpectedClaimRowVersion is null || !decision.ExpectedClaimRowVersion.SequenceEqual(row.RowVersion!)))
                    throw new AdmissionQueueConcurrencyException("Outstanding claim changed; refresh the closing review.");
            }

            foreach (var row in rows)
            {
                var decision = command.Decisions.Single(x => x.AntrianId == row.AntrianId && x.NoUrut == row.NoUrut);
                var reason = decision.Disposition == AdmissionQueueClosingDispositions.NoShow ? "NoShow" : decision.Reason!.Trim();
                if (row.ClaimState == (int)AdmissionQueueClaimState.Outstanding)
                {
                    var released = connection.Execute("""
                        UPDATE BILRG_AdmLoketCurrentCall SET ClaimState=0, IsActive=0, ReleasedAt=@ClosedAt, UpdUser=@UserId, UpdDate=@ClosedAt
                        WHERE LoketKey=@LoketKey AND AntrianId=@AntrianId AND NoUrut=@NoUrut AND ClaimState=1 AND IsActive=1 AND RowVersion=@RowVersion;
                        """, new { row.LoketKey, row.AntrianId, row.NoUrut, RowVersion = decision.ExpectedClaimRowVersion, ClosedAt = closedAt, command.UserId }, transaction);
                    if (released != 1) throw new AdmissionQueueConcurrencyException("Outstanding claim changed concurrently.");
                }
                var withdrawn = connection.Execute("""
                    UPDATE BILRG_AntrianEntry SET AntrianStatus=3, WithdrawalReason=@Reason, WithdrawalUserId=@UserId, WithdrawnAt=@ClosedAt, UpdUser=@UserId, UpdDate=@ClosedAt
                    WHERE AntrianId=@AntrianId AND NoUrut=@NoUrut AND AntrianStatus=0;
                    """, new { row.AntrianId, row.NoUrut, Reason = reason, command.UserId, ClosedAt = closedAt }, transaction);
                if (withdrawn != 1) throw new AdmissionQueueConcurrencyException("Queue entry changed concurrently.");
                connection.Execute("""
                    UPDATE BILRG_AdmBookingAssistance SET IsActive=0, UpdUser=@UserId, UpdDate=@ClosedAt
                    WHERE AntrianId=@AntrianId AND NoUrut=@NoUrut AND IsActive=1;
                    """, new { row.AntrianId, row.NoUrut, command.UserId, ClosedAt = closedAt }, transaction);
                InsertAudit(connection, transaction, AuditLog.Create(command.UserId, closedAt, "ADMISSION_QUEUE_CLOSE_ENTRY", "AdmissionQueueEntry", Key(row.AntrianId, row.NoUrut), reason,
                    AuditLogSnapshotJson.Serialize(new { row.LoketKey, ClaimState = row.ClaimState, decision.Disposition })));
            }

            var remaining = connection.ExecuteScalar<int>("""
                SELECT COUNT(1) FROM BILRG_Antrian q INNER JOIN BILRG_AntrianEntry e ON e.AntrianId=q.AntrianId
                WHERE q.AntrianDate=@BusinessDate AND q.ServicePointCode=@ServicePointId AND e.AntrianStatus=0;
                """, new { BusinessDate = businessDate.ToDateTime(TimeOnly.MinValue), command.ServicePointId }, transaction);
            if (remaining != 0) throw new AdmissionQueueConcurrencyException("Remaining queue entries changed; refresh the closing review.");
            var noShow = command.Decisions.Count(x => x.Disposition == AdmissionQueueClosingDispositions.NoShow);
            var withdrawnCount = command.Decisions.Count - noShow;
            InsertAudit(connection, transaction, AuditLog.Create(command.UserId, closedAt, "ADMISSION_QUEUE_CLOSE", "AdmissionServicePointBusinessDate", $"{command.ServicePointId}:{businessDate:yyyy-MM-dd}", null,
                AuditLogSnapshotJson.Serialize(new { SubmittedCount = command.Decisions.Count, NoShowCount = noShow, WithdrawnCount = withdrawnCount })));
            transaction.Commit();
            return new AdmissionQueueCloseResponse(businessDate, command.ServicePointId, command.Decisions.Count, noShow, withdrawnCount, closedAt);
        }
        catch { transaction.Rollback(); throw; }
    }

    private static void InsertAudit(SqlConnection connection, SqlTransaction transaction, AuditLog audit) => connection.Execute("""
        INSERT INTO BILRG_AuditLog (AuditId,EventTime,UserId,ActionType,EntityName,EntityId,Reason,OriginalDataJson,CorrelationId,ClientIpAddress,UserAgent)
        VALUES (@AuditId,@EventTime,@UserId,@ActionType,@EntityName,@EntityId,@Reason,@OriginalDataJson,@CorrelationId,@ClientIpAddress,@UserAgent)
        """, audit, transaction);
    private static string Key(string id, int number) => $"{id}:{number}";
    private static AdmissionQueueClosingPreviewItem ToPreview(Row row) => new(row.AntrianId, row.NoUrut, row.QueueLabel, EmptyToNull(row.PersonName), EmptyToNull(row.ReffId), EmptyToNull(row.ReffDesc), row.CallCount, row.CalledAt, row.ClaimState is null ? null : (AdmissionQueueClaimState)row.ClaimState, row.LoketKey, row.RowVersion is null ? null : Convert.ToBase64String(row.RowVersion), [AdmissionQueueClosingDispositions.NoShow, AdmissionQueueClosingDispositions.Withdraw]);
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private sealed record Row(string AntrianId, int NoUrut, string? QueueLabel, string PersonName, string ReffId, string ReffDesc, int CallCount, DateTime? CalledAt, int? ClaimState, string? LoketKey, byte[]? RowVersion);
    private const string SelectSql = """
        SELECT e.AntrianId,e.NoUrut,CASE WHEN q.QueuePrefixSnapshot='' THEN NULL ELSE q.QueuePrefixSnapshot+RIGHT('0000'+CONVERT(VARCHAR(4),e.NoUrut),4) END QueueLabel,e.PersonName,e.ReffId,e.ReffDesc,e.CallCount,NULLIF(c.CalledAt,'3000-01-01') CalledAt,c.ClaimState,c.LoketKey,c.RowVersion
        FROM BILRG_Antrian q INNER JOIN BILRG_AntrianEntry e ON e.AntrianId=q.AntrianId LEFT JOIN BILRG_AdmLoketCurrentCall c ON c.AntrianId=e.AntrianId AND c.NoUrut=e.NoUrut AND c.IsActive=1
        WHERE q.AntrianDate=@BusinessDate AND q.ServicePointCode=@ServicePointId AND e.AntrianStatus=0 ORDER BY e.Priority DESC,e.CreatedAt,e.NoUrut,e.AntrianId;
        """;
    private const string LockedSelectSql = """
        SELECT e.AntrianId,e.NoUrut,CASE WHEN q.QueuePrefixSnapshot='' THEN NULL ELSE q.QueuePrefixSnapshot+RIGHT('0000'+CONVERT(VARCHAR(4),e.NoUrut),4) END QueueLabel,e.PersonName,e.ReffId,e.ReffDesc,e.CallCount,NULLIF(c.CalledAt,'3000-01-01') CalledAt,c.ClaimState,c.LoketKey,c.RowVersion
        FROM BILRG_Antrian q WITH (UPDLOCK,HOLDLOCK) INNER JOIN BILRG_AntrianEntry e WITH (UPDLOCK,HOLDLOCK) ON e.AntrianId=q.AntrianId LEFT JOIN BILRG_AdmLoketCurrentCall c WITH (UPDLOCK,HOLDLOCK) ON c.AntrianId=e.AntrianId AND c.NoUrut=e.NoUrut AND c.IsActive=1
        WHERE q.AntrianDate=@BusinessDate AND q.ServicePointCode=@ServicePointId AND e.AntrianStatus=0 ORDER BY e.Priority DESC,e.CreatedAt,e.NoUrut,e.AntrianId;
        """;
}
