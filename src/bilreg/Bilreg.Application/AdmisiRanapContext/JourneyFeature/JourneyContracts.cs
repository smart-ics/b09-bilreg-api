using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;

namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Normalized Release 1 facts for one candidate journey. Built by the future projection (B2);
/// the pure resolver never loads persistence.
/// </summary>
public sealed record JourneyNormalizedFacts(
    JourneyOriginKind OriginKind,
    JourneyPatientSummary? Patient,
    OpnameRequestJourneyFact? OpnameRequest,
    ReservationJourneyFact? Reservation,
    IReadOnlyList<AdmissionJourneyFact> Admissions,
    IReadOnlyList<WaitingListJourneyFact> WaitingLists,
    bool HasOrphanRegistration = false)
{
    public static JourneyNormalizedFacts Empty(JourneyOriginKind origin) =>
        new(origin, null, null, null, Array.Empty<AdmissionJourneyFact>(), Array.Empty<WaitingListJourneyFact>());
}

public sealed record OpnameRequestJourneyFact(
    string OpnameRequestId,
    OpnameRequestStatusEnum Status,
    string FulfilledRegId,
    DateTime? StatusAt = null);

public sealed record ReservationJourneyFact(
    string ReservationId,
    ReservationStatusEnum Status,
    string RealizedRegId,
    DateTime? StatusAt = null);

public sealed record AdmissionJourneyFact(
    string RegId,
    AdmissionStatusEnum Status,
    string OpnameRequestId,
    string ReservationId,
    DateTime? AdmissionAt = null,
    string? BangsalId = null,
    string? BangsalName = null);

public sealed record WaitingListJourneyFact(
    string WaitingListId,
    string RegId,
    WaitingListStatusEnum Status,
    DateTime? StatusAt = null,
    string? BangsalId = null,
    string? BangsalName = null)
{
    public bool IsActive =>
        Status is WaitingListStatusEnum.Waiting or WaitingListStatusEnum.Accepted;
}

public sealed record JourneyPatientSummary(
    string PatientId,
    string MedicalRecordNumber,
    string DisplayName,
    string? Sex = null,
    DateOnly? BirthDate = null,
    string? AgeDisplay = null,
    string? ContactSummary = null);

public sealed record JourneyCurrentCondition(
    string Title,
    string Explanation,
    DateTime? Since);

public sealed record JourneyTaskOwner(
    JourneyOwnerDomain Domain,
    string? UnitId,
    string? Name);

public sealed record JourneyNextTask(
    JourneyActionCode Code,
    string Label,
    JourneyTaskOwner Owner,
    bool CanExecute,
    string? BlockedReason,
    string? RequiredPermission);

public sealed record JourneyAllowedAction(
    JourneyActionCode Code,
    string Label,
    bool CanExecute,
    string? BlockedReason,
    string? RequiredPermission);

public sealed record JourneyAccommodationHandoverSummary(
    string? ActiveWaitingListId,
    WaitingListStatusEnum? ActiveStatus,
    string? TargetBangsalId,
    string? TargetBangsalName,
    int WaitingListCount,
    int ClosedCount,
    int CancelledCount);

public sealed record JourneyPlacementAvailability(
    JourneyPlacementAvailabilityStatus Status,
    JourneyPlacementUnavailableReasonCode ReasonCode,
    JourneyTaskOwner? ResponsibleWard,
    string Explanation);

public sealed record JourneyTimelineEvent(
    JourneyTimelineEventKind Kind,
    DateTime? OccurredAt,
    string? RelatedRecordId,
    string Summary);

public sealed record JourneySystemAuditReferences(
    JourneyOriginKind OriginKind,
    string? OpnameRequestId,
    OpnameRequestStatusEnum? OpnameRequestStatus,
    string? ReservationId,
    ReservationStatusEnum? ReservationStatus,
    string? RegId,
    AdmissionStatusEnum? AdmissionStatus,
    IReadOnlyList<string> WaitingListIds,
    DateTime? ProjectedAt);

public sealed record JourneyReconciliationIssue(
    string Code,
    string Message,
    IReadOnlyList<string> RelatedRecordIds);

public sealed record JourneyIdentity(
    string JourneyId,
    string? RegId,
    JourneyOriginKind OriginKind,
    bool IsProspective,
    bool IsRegistered,
    bool IsTerminal);

/// <summary>
/// Pure Release 1 resolution result: identity, stage, condition, next task, candidate actions
/// (stage/ownership boundary only), and supporting contract sections from normalized facts.
/// Final executability for command dependencies is applied by the projection (see
/// <see cref="JourneyAllowedActionEvaluator"/>).
/// </summary>
public sealed record Release1JourneyResolution(
    JourneyIdentity Identity,
    JourneyOperationalStage Stage,
    JourneyCurrentCondition CurrentCondition,
    JourneyNextTask? NextTask,
    IReadOnlyList<JourneyAllowedAction> CandidateAdmisiActions,
    JourneyAccommodationHandoverSummary HandoverSummary,
    JourneyPlacementAvailability PlacementAvailability,
    IReadOnlyList<JourneyTimelineEvent> Timeline,
    IReadOnlyList<JourneyAttentionFlag> AttentionFlags,
    JourneySystemAuditReferences SystemAudit,
    IReadOnlyList<JourneyReconciliationIssue> ReconciliationIssues,
    int ProjectionVersion = JourneyProjectionVersions.Release1);
