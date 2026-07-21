using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;

namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

public sealed record JourneyIntegrityResult(
    IReadOnlyList<JourneyReconciliationIssue> Issues)
{
    public static JourneyIntegrityResult Empty { get; } =
        new(Array.Empty<JourneyReconciliationIssue>());

    public bool HasBlockingIssues => Issues.Count > 0;
}

/// <summary>
/// Validates Release 1 relationship integrity. Never merges or discards conflicting facts.
/// </summary>
public static class JourneyIntegrityValidator
{
    public static JourneyIntegrityResult Validate(JourneyNormalizedFacts facts)
    {
        var issues = new List<JourneyReconciliationIssue>();

        if (facts.HasOrphanRegistration)
        {
            issues.Add(Issue(
                JourneyReconciliationCodes.OrphanRegistration,
                "A Registration exists without a matching Admission for the episode.",
                Array.Empty<string>()));
        }

        ValidateAdmissionSources(facts, issues);
        ValidateSourceToAdmissionLinks(facts, issues);
        ValidateWaitingLists(facts, issues);
        ValidateCancellationContradictions(facts, issues);
        ValidateUnexpectedCompleted(facts, issues);

        return new JourneyIntegrityResult(issues);
    }

    private static void ValidateAdmissionSources(
        JourneyNormalizedFacts facts,
        List<JourneyReconciliationIssue> issues)
    {
        foreach (var admission in facts.Admissions)
        {
            var hasOpn = JourneyIdFactory.HasValue(admission.OpnameRequestId);
            var hasRsv = JourneyIdFactory.HasValue(admission.ReservationId);
            if (hasOpn && hasRsv)
            {
                issues.Add(Issue(
                    JourneyReconciliationCodes.AdmissionHasBothSources,
                    $"Admission {admission.RegId} references both an Opname Request and a Reservation.",
                    [admission.RegId, admission.OpnameRequestId, admission.ReservationId]));
            }
        }

        if (facts.Admissions.Count > 1)
        {
            issues.Add(Issue(
                JourneyReconciliationCodes.MultipleAdmissionsForSource,
                "Multiple Admissions are linked to the same journey source or facts bag.",
                facts.Admissions.Select(a => a.RegId).ToArray()));
        }
    }

    private static void ValidateSourceToAdmissionLinks(
        JourneyNormalizedFacts facts,
        List<JourneyReconciliationIssue> issues)
    {
        if (facts.OpnameRequest is { Status: OpnameRequestStatusEnum.Fulfilled } opn)
        {
            if (!JourneyIdFactory.HasValue(opn.FulfilledRegId))
            {
                issues.Add(Issue(
                    JourneyReconciliationCodes.FulfilledSourceWithoutAdmission,
                    $"Opname Request {opn.OpnameRequestId} is Fulfilled without a FulfilledRegId.",
                    [opn.OpnameRequestId]));
            }
            else
            {
                var matches = facts.Admissions
                    .Where(a => a.RegId == opn.FulfilledRegId)
                    .ToList();
                if (matches.Count == 0)
                {
                    issues.Add(Issue(
                        JourneyReconciliationCodes.FulfilledSourceWithoutAdmission,
                        $"Opname Request {opn.OpnameRequestId} is Fulfilled to {opn.FulfilledRegId} but that Admission is missing.",
                        [opn.OpnameRequestId, opn.FulfilledRegId]));
                }
                else
                {
                    foreach (var admission in matches)
                    {
                        if (JourneyIdFactory.HasValue(admission.OpnameRequestId)
                            && admission.OpnameRequestId != opn.OpnameRequestId)
                        {
                            issues.Add(Issue(
                                JourneyReconciliationCodes.SourceFulfillmentRegIdMismatch,
                                $"Admission {admission.RegId} OpnameRequestId does not match fulfilled source {opn.OpnameRequestId}.",
                                [opn.OpnameRequestId, admission.RegId, admission.OpnameRequestId]));
                        }

                        if (!JourneyIdFactory.HasValue(admission.OpnameRequestId))
                        {
                            issues.Add(Issue(
                                JourneyReconciliationCodes.SourceFulfillmentRegIdMismatch,
                                $"Admission {admission.RegId} is missing the Opname Request link expected from fulfilled source {opn.OpnameRequestId}.",
                                [opn.OpnameRequestId, admission.RegId]));
                        }
                    }
                }

                if (facts.Admissions.Count(a =>
                        JourneyIdFactory.HasValue(a.OpnameRequestId)
                        && a.OpnameRequestId == opn.OpnameRequestId) > 1)
                {
                    issues.Add(Issue(
                        JourneyReconciliationCodes.MultipleAdmissionsForSource,
                        $"Opname Request {opn.OpnameRequestId} resolves to more than one Admission.",
                        facts.Admissions
                            .Where(a => a.OpnameRequestId == opn.OpnameRequestId)
                            .Select(a => a.RegId)
                            .Prepend(opn.OpnameRequestId)
                            .ToArray()));
                }
            }
        }

        if (facts.Reservation is { Status: ReservationStatusEnum.Realized } rsv)
        {
            if (!JourneyIdFactory.HasValue(rsv.RealizedRegId))
            {
                issues.Add(Issue(
                    JourneyReconciliationCodes.FulfilledSourceWithoutAdmission,
                    $"Reservation {rsv.ReservationId} is Realized without a RealizedRegId.",
                    [rsv.ReservationId]));
            }
            else
            {
                var matches = facts.Admissions
                    .Where(a => a.RegId == rsv.RealizedRegId)
                    .ToList();
                if (matches.Count == 0)
                {
                    issues.Add(Issue(
                        JourneyReconciliationCodes.FulfilledSourceWithoutAdmission,
                        $"Reservation {rsv.ReservationId} is Realized to {rsv.RealizedRegId} but that Admission is missing.",
                        [rsv.ReservationId, rsv.RealizedRegId]));
                }
                else
                {
                    foreach (var admission in matches)
                    {
                        if (JourneyIdFactory.HasValue(admission.ReservationId)
                            && admission.ReservationId != rsv.ReservationId)
                        {
                            issues.Add(Issue(
                                JourneyReconciliationCodes.SourceFulfillmentRegIdMismatch,
                                $"Admission {admission.RegId} ReservationId does not match realized source {rsv.ReservationId}.",
                                [rsv.ReservationId, admission.RegId, admission.ReservationId]));
                        }

                        if (!JourneyIdFactory.HasValue(admission.ReservationId))
                        {
                            issues.Add(Issue(
                                JourneyReconciliationCodes.SourceFulfillmentRegIdMismatch,
                                $"Admission {admission.RegId} is missing the Reservation link expected from realized source {rsv.ReservationId}.",
                                [rsv.ReservationId, admission.RegId]));
                        }
                    }
                }

                if (facts.Admissions.Count(a =>
                        JourneyIdFactory.HasValue(a.ReservationId)
                        && a.ReservationId == rsv.ReservationId) > 1)
                {
                    issues.Add(Issue(
                        JourneyReconciliationCodes.MultipleAdmissionsForSource,
                        $"Reservation {rsv.ReservationId} resolves to more than one Admission.",
                        facts.Admissions
                            .Where(a => a.ReservationId == rsv.ReservationId)
                            .Select(a => a.RegId)
                            .Prepend(rsv.ReservationId)
                            .ToArray()));
                }
            }
        }
    }

    private static void ValidateWaitingLists(
        JourneyNormalizedFacts facts,
        List<JourneyReconciliationIssue> issues)
    {
        var episodeRegIds = facts.Admissions
            .Select(a => a.RegId)
            .Where(JourneyIdFactory.HasValue)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var wl in facts.WaitingLists)
        {
            if (!JourneyIdFactory.HasValue(wl.RegId))
            {
                issues.Add(Issue(
                    JourneyReconciliationCodes.OrphanWaitingList,
                    $"Waiting List {wl.WaitingListId} has no RegId.",
                    [wl.WaitingListId]));
                continue;
            }

            if (episodeRegIds.Count == 0)
            {
                issues.Add(Issue(
                    JourneyReconciliationCodes.OrphanWaitingList,
                    $"Waiting List {wl.WaitingListId} references RegId {wl.RegId} but no Admission is present.",
                    [wl.WaitingListId, wl.RegId]));
                continue;
            }

            if (!episodeRegIds.Contains(wl.RegId))
            {
                issues.Add(Issue(
                    JourneyReconciliationCodes.WaitingListRegIdMismatch,
                    $"Waiting List {wl.WaitingListId} RegId {wl.RegId} does not match the episode Admission(s).",
                    [wl.WaitingListId, wl.RegId, .. episodeRegIds]));
            }
        }

        var active = facts.WaitingLists.Where(w => w.IsActive).ToList();
        if (active.Count > 1)
        {
            issues.Add(Issue(
                JourneyReconciliationCodes.MultipleActiveWaitingLists,
                "More than one active Waiting List exists for the journey.",
                active.Select(w => w.WaitingListId).ToArray()));
        }
    }

    private static void ValidateCancellationContradictions(
        JourneyNormalizedFacts facts,
        List<JourneyReconciliationIssue> issues)
    {
        var admission = JourneyIdFactory.SelectPrimaryAdmission(facts);
        if (admission is null)
            return;

        var activeWls = facts.WaitingLists.Where(w => w.IsActive).ToList();
        if (admission.Status == AdmissionStatusEnum.Cancelled && activeWls.Count > 0)
        {
            issues.Add(Issue(
                JourneyReconciliationCodes.ActiveWaitingListOnCancelledAdmission,
                $"Admission {admission.RegId} is Cancelled but still has an active Waiting List.",
                activeWls.Select(w => w.WaitingListId).Prepend(admission.RegId).ToArray()));
        }

        var sourceCancelled =
            facts.OpnameRequest?.Status == OpnameRequestStatusEnum.Cancelled
            || facts.Reservation?.Status == ReservationStatusEnum.Cancelled;

        if (sourceCancelled
            && admission.Status is not AdmissionStatusEnum.Cancelled
                and not AdmissionStatusEnum.Completed
            && IsActiveAdmission(admission.Status))
        {
            issues.Add(Issue(
                JourneyReconciliationCodes.ActiveAdmissionOnCancelledSource,
                $"Source is Cancelled while Admission {admission.RegId} remains active.",
                [admission.RegId]));
        }
    }

    private static void ValidateUnexpectedCompleted(
        JourneyNormalizedFacts facts,
        List<JourneyReconciliationIssue> issues)
    {
        foreach (var admission in facts.Admissions.Where(a => a.Status == AdmissionStatusEnum.Completed))
        {
            issues.Add(Issue(
                JourneyReconciliationCodes.AdmissionCompletedWithoutAuthoritativeProcess,
                $"Admission {admission.RegId} is Completed, but Release 1 has no authoritative completion process that owns this state.",
                [admission.RegId]));
        }
    }

    private static bool IsActiveAdmission(AdmissionStatusEnum status) =>
        status is AdmissionStatusEnum.Admitted
            or AdmissionStatusEnum.Updated
            or AdmissionStatusEnum.Waiting;

    private static JourneyReconciliationIssue Issue(
        string code,
        string message,
        IReadOnlyList<string> related) =>
        new(code, message, related);
}
