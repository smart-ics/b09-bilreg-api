using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;

namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// SQL-parity stage inputs and derivation. Must stay aligned with
/// <see cref="Release1JourneyStageResolver"/> DecideStage precedence.
/// Used for list filter / facets / cursor pagination in SQL and for parity tests.
/// </summary>
public sealed record Release1SqlStageInputs(
    bool HasBlockingIntegrity,
    bool AdmissionCancelled,
    bool ProspectiveSourceCancelled,
    bool AdmissionCompletedWithoutAuthority,
    WaitingListStatusEnum? ActiveWaitingListStatus,
    bool HasClosedHandover,
    bool HasActiveAdmission,
    bool ProspectiveOpnameRequested,
    bool ProspectiveReservationReady);

/// <summary>
/// Isolated Release 1 SQL-stage derivation. Documented precedence:
/// 1. Blocking integrity → NEEDS_RECONCILIATION
/// 2. Authoritative cancellation → CANCELLED
/// 3. Admission.Completed (no owning process) → treated as integrity (caller sets HasBlockingIntegrity)
/// 4. Active WL Waiting → WARD_ACCEPTANCE_REQUIRED
/// 5. Active WL Accepted → HANDOVER_ACCEPTED
/// 6. Closed handover + active Admission → PLACEMENT_STATUS_UNAVAILABLE
/// 7. Active Admission → HANDOVER_REQUIRED
/// 8. Prospective Requested / Reserved|Maintained → REGISTRATION_REQUIRED
/// 9. Else → NEEDS_RECONCILIATION
/// </summary>
public static class Release1JourneySqlStageExpression
{
    public static JourneyOperationalStage Derive(Release1SqlStageInputs i)
    {
        if (i.HasBlockingIntegrity || i.AdmissionCompletedWithoutAuthority)
            return JourneyOperationalStage.NeedsReconciliation;

        if (i.AdmissionCancelled || i.ProspectiveSourceCancelled)
            return JourneyOperationalStage.Cancelled;

        if (i.ActiveWaitingListStatus == WaitingListStatusEnum.Waiting)
            return JourneyOperationalStage.WardAcceptanceRequired;

        if (i.ActiveWaitingListStatus == WaitingListStatusEnum.Accepted)
            return JourneyOperationalStage.HandoverAccepted;

        if (i.HasClosedHandover && i.HasActiveAdmission)
            return JourneyOperationalStage.PlacementStatusUnavailable;

        if (i.HasActiveAdmission)
            return JourneyOperationalStage.HandoverRequired;

        if (i.ProspectiveOpnameRequested || i.ProspectiveReservationReady)
            return JourneyOperationalStage.RegistrationRequired;

        return JourneyOperationalStage.NeedsReconciliation;
    }

    /// <summary>
    /// Builds SQL-stage inputs from the same normalized facts the B1 resolver uses.
    /// Parity tests compare <see cref="Derive"/> with <see cref="Release1JourneyStageResolver"/>.
    /// </summary>
    public static Release1SqlStageInputs FromNormalizedFacts(JourneyNormalizedFacts facts)
    {
        var integrity = JourneyIntegrityValidator.Validate(facts);
        var admission = JourneyIdFactory.SelectPrimaryAdmission(facts);
        var activeWl = facts.WaitingLists
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.StatusAt)
            .FirstOrDefault();

        var hasActiveAdmission = admission is not null && IsActiveAdmission(admission.Status);
        var prospectiveOpnameRequested =
            admission is null
            && facts.OpnameRequest is { Status: OpnameRequestStatusEnum.Requested };
        var prospectiveReservationReady =
            admission is null
            && facts.Reservation is
            {
                Status: ReservationStatusEnum.Reserved or ReservationStatusEnum.Maintained
            };
        var prospectiveSourceCancelled =
            admission is null
            && (facts.OpnameRequest?.Status == OpnameRequestStatusEnum.Cancelled
                || facts.Reservation?.Status == ReservationStatusEnum.Cancelled);

        // Completed is always a blocking integrity issue in Release 1.
        var completedWithoutAuthority =
            facts.Admissions.Any(a => a.Status == AdmissionStatusEnum.Completed);

        return new Release1SqlStageInputs(
            HasBlockingIntegrity: integrity.HasBlockingIssues,
            AdmissionCancelled: admission?.Status == AdmissionStatusEnum.Cancelled,
            ProspectiveSourceCancelled: prospectiveSourceCancelled,
            AdmissionCompletedWithoutAuthority: completedWithoutAuthority,
            ActiveWaitingListStatus: activeWl?.Status,
            HasClosedHandover: facts.WaitingLists.Any(w => w.Status == WaitingListStatusEnum.Closed),
            HasActiveAdmission: hasActiveAdmission,
            ProspectiveOpnameRequested: prospectiveOpnameRequested,
            ProspectiveReservationReady: prospectiveReservationReady);
    }

    /// <summary>
    /// SQL CASE expression column aliases expected on the journey facts projection.
    /// Integer stage values match <see cref="JourneyOperationalStage"/>.
    /// </summary>
    public const string SqlCaseExpression = """
        CASE
            WHEN j.HasBlockingIntegrity = 1 OR j.AdmissionCompletedWithoutAuthority = 1
                THEN 7 /* NeedsReconciliation */
            WHEN j.AdmissionCancelled = 1 OR j.ProspectiveSourceCancelled = 1
                THEN 6 /* Cancelled */
            WHEN j.ActiveWaitingListStatus = 0
                THEN 2 /* WardAcceptanceRequired */
            WHEN j.ActiveWaitingListStatus = 1
                THEN 3 /* HandoverAccepted */
            WHEN j.HasClosedHandover = 1 AND j.HasActiveAdmission = 1
                THEN 4 /* PlacementStatusUnavailable */
            WHEN j.HasActiveAdmission = 1
                THEN 1 /* HandoverRequired */
            WHEN j.ProspectiveOpnameRequested = 1 OR j.ProspectiveReservationReady = 1
                THEN 0 /* RegistrationRequired */
            ELSE 7 /* NeedsReconciliation */
        END
        """;

    private static bool IsActiveAdmission(AdmissionStatusEnum status) =>
        status is AdmissionStatusEnum.Admitted
            or AdmissionStatusEnum.Updated
            or AdmissionStatusEnum.Waiting;
}
