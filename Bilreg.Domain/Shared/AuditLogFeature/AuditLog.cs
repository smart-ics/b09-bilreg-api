using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.Shared.AuditLogFeature;

public class AuditLog
{
    private static readonly DateTime EmptyDateSentinel = new(3000, 1, 1);

    private AuditLog()
    {
    }

    private AuditLog(
        string auditId,
        DateTime eventTime,
        string userId,
        string actionType,
        string entityName,
        string entityId,
        string? reason,
        string? originalDataJson,
        string? correlationId,
        string? clientIpAddress,
        string? userAgent)
    {
        AuditId = auditId;
        EventTime = eventTime;
        UserId = userId;
        ActionType = actionType;
        EntityName = entityName;
        EntityId = entityId;
        Reason = reason;
        OriginalDataJson = originalDataJson;
        CorrelationId = correlationId;
        ClientIpAddress = clientIpAddress;
        UserAgent = userAgent;
    }

    #region CREATION

    public static AuditLog Create(
        string userId,
        string actionType,
        string entityName,
        string entityId,
        string? reason = null,
        string? originalDataJson = null,
        string? correlationId = null,
        string? clientIpAddress = null,
        string? userAgent = null)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        ValidateActionAndEntity(actionType, entityName, entityId);

        return CreateCore(
            userId,
            DateTime.UtcNow,
            actionType,
            entityName,
            entityId,
            reason,
            originalDataJson,
            correlationId,
            clientIpAddress,
            userAgent);
    }

    public static AuditLog Create(
        AuditInfoType auditInfo,
        string actionType,
        string entityName,
        string entityId,
        string? reason = null,
        string? originalDataJson = null,
        string? correlationId = null,
        string? clientIpAddress = null,
        string? userAgent = null)
    {
        ValidateAuditInfo(auditInfo);
        ValidateActionAndEntity(actionType, entityName, entityId);

        return CreateCore(
            auditInfo.UserId,
            auditInfo.Timestamp,
            actionType,
            entityName,
            entityId,
            reason,
            originalDataJson,
            correlationId,
            clientIpAddress,
            userAgent);
    }

    public static AuditLog Create(
        AuditTrailType auditTrail,
        AuditLogEventSource eventSource,
        string actionType,
        string entityName,
        string entityId,
        string? reason = null,
        string? originalDataJson = null,
        string? correlationId = null,
        string? clientIpAddress = null,
        string? userAgent = null)
    {
        Guard.Against.Null(auditTrail);

        var auditInfo = eventSource switch
        {
            AuditLogEventSource.Created => auditTrail.Created,
            AuditLogEventSource.Modified => auditTrail.Modified,
            AuditLogEventSource.Voided => auditTrail.Voided,
            _ => throw new ArgumentOutOfRangeException(nameof(eventSource), eventSource, null)
        };

        return Create(
            auditInfo,
            actionType,
            entityName,
            entityId,
            reason,
            originalDataJson,
            correlationId,
            clientIpAddress,
            userAgent);
    }

    #endregion

    #region PROPERTIES

    public string AuditId { get; private set; } = string.Empty;
    public DateTime EventTime { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string ActionType { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public string? OriginalDataJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? ClientIpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    #endregion

    private static AuditLog CreateCore(
        string userId,
        DateTime eventTime,
        string actionType,
        string entityName,
        string entityId,
        string? reason,
        string? originalDataJson,
        string? correlationId,
        string? clientIpAddress,
        string? userAgent)
    {
        return new AuditLog(
            NunaId.New("AUD"),
            eventTime,
            userId,
            actionType,
            entityName,
            entityId,
            reason,
            originalDataJson,
            correlationId,
            clientIpAddress,
            userAgent);
    }

    private static void ValidateActionAndEntity(string actionType, string entityName, string entityId)
    {
        Guard.Against.NullOrWhiteSpace(actionType);
        Guard.Against.NullOrWhiteSpace(entityName);
        Guard.Against.NullOrWhiteSpace(entityId);
    }

    private static void ValidateAuditInfo(AuditInfoType auditInfo)
    {
        Guard.Against.Null(auditInfo);

        if (string.IsNullOrWhiteSpace(auditInfo.UserId)
            || auditInfo.Timestamp.Date == EmptyDateSentinel.Date)
        {
            throw new ArgumentException("AuditInfoType must not be Default.", nameof(auditInfo));
        }
    }
}
