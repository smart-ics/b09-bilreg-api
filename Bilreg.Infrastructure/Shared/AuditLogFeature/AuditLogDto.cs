using Bilreg.Domain.Shared.AuditLogFeature;

namespace Bilreg.Infrastructure.Shared.AuditLogFeature;

public record AuditLogDto(
    string AuditId,
    DateTime EventTime,
    string UserId,
    string ActionType,
    string EntityName,
    string EntityId,
    string Reason,
    string OriginalDataJson,
    string CorrelationId,
    string ClientIpAddress,
    string UserAgent)
{
    public static AuditLogDto FromModel(AuditLog model)
    {
        return new AuditLogDto(
            model.AuditId,
            model.EventTime,
            model.UserId,
            model.ActionType,
            model.EntityName,
            model.EntityId,
            model.Reason ?? string.Empty,
            model.OriginalDataJson ?? string.Empty,
            model.CorrelationId ?? string.Empty,
            model.ClientIpAddress ?? string.Empty,
            model.UserAgent ?? string.Empty);
    }
}
