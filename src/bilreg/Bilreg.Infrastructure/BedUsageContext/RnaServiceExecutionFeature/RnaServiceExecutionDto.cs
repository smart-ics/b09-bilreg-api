using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

namespace Bilreg.Infrastructure.BedUsageContext.RnaServiceExecutionFeature;

public record RnaServiceExecutionDto(
    string ServiceExecutionId, string RegistrationId, string PatientId, string CareContextId,
    string ResponsibleWardId, int ExecutionSource, string ClinicalOrderId, string OrderOccurrenceId,
    string FulfilmentObligationId, string SourceContext, string SourceFactId, int SourceRevision,
    string AssignedPerformerId, int WorkStatus, bool HasExecutionAuthority,
    string AuthorityBasisReference, bool RequiresSubsequentAuthorization,
    DateTime? AuthorizationDueAt, int SubsequentAuthorizationStatus, int Version,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static RnaServiceExecutionDto FromModel(RnaServiceExecutionModel model)
    {
        var audit = model.ListSourceRevision
            .Select(x => new AuditFact(x.ActorId, x.RecordedAt))
            .Concat(model.ListExecutionFact.Select(x => new AuditFact(x.RecorderActorId, x.RecordedAt)))
            .Concat(model.ListCorrection.Select(x => new AuditFact(x.CorrectingActorId, x.RecordedAt)))
            .OrderBy(x => x.At)
            .ToList();
        var first = audit.FirstOrDefault() ?? new AuditFact(string.Empty, EmptyDate);
        var last = audit.LastOrDefault() ?? first;
        var authority = model.ExecutionAuthority;

        return new RnaServiceExecutionDto(
            model.ServiceExecutionId, model.RegistrationId, model.PatientId, model.CareContextId,
            model.ResponsibleWardId, (int)model.ExecutionSource, model.ClinicalOrderId,
            model.OrderOccurrenceId, model.FulfilmentObligationId, model.SourceContext,
            model.SourceFactId, model.SourceRevision, model.AssignedPerformerId,
            (int)model.WorkStatus, authority is not null,
            authority?.AuthorityBasisReference ?? string.Empty,
            authority?.RequiresSubsequentAuthorization ?? false,
            authority?.AuthorizationDueAt, (int)(authority?.SubsequentAuthorizationStatus ??
                SubsequentAuthorizationStatusEnum.NotRequired), model.Version,
            first.ActorId, first.At, last.ActorId, last.At, string.Empty, EmptyDate);
    }

    public RnaServiceExecutionModel ToModel(
        IEnumerable<ServiceWorkSourceRevisionType> revisions,
        IEnumerable<ServiceExecutionFactType> facts,
        IEnumerable<ExecutionCorrectionModel> corrections) => new(
        ServiceExecutionId, RegistrationId, PatientId, CareContextId, ResponsibleWardId,
        (ExecutionSourceEnum)ExecutionSource, ClinicalOrderId, OrderOccurrenceId,
        FulfilmentObligationId, SourceContext, SourceFactId, SourceRevision,
        AssignedPerformerId, (RnaServiceWorkStatusEnum)WorkStatus,
        HasExecutionAuthority
            ? new ExecutionAuthorityType(AuthorityBasisReference, RequiresSubsequentAuthorization,
                AuthorizationDueAt is null ? null : AsUtc(AuthorizationDueAt.Value),
                (SubsequentAuthorizationStatusEnum)SubsequentAuthorizationStatus)
            : null,
        Version, revisions, facts, corrections, []);

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private sealed record AuditFact(string ActorId, DateTime At);
}
