using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;

namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Pure Release 1 stage / owner / next-task resolver.
/// Precedence: integrity → cancellation → (completion unreachable) → active WL Waiting →
/// active WL Accepted → closed handover without placement → active Admission handover →
/// unregistered prospective source.
/// </summary>
public static class Release1JourneyStageResolver
{
    public static Release1JourneyResolution Resolve(JourneyNormalizedFacts facts)
    {
        var integrity = JourneyIntegrityValidator.Validate(facts);
        return Resolve(facts, integrity);
    }

    public static Release1JourneyResolution Resolve(
        JourneyNormalizedFacts facts,
        JourneyIntegrityResult integrity)
    {
        var journeyId = JourneyIdFactory.Derive(facts, integrity);
        var admission = JourneyIdFactory.SelectPrimaryAdmission(facts);
        var stageDecision = DecideStage(facts, integrity, admission);
        var handover = BuildHandoverSummary(facts);
        var placement = BuildPlacementAvailability(facts, admission, stageDecision.Stage);
        var timeline = BuildTimeline(facts);
        var identity = BuildIdentity(journeyId, facts, admission, stageDecision.Stage);
        var audit = BuildAudit(facts, admission);
        var candidates = BuildCandidateActions(facts, admission, stageDecision);

        return new Release1JourneyResolution(
            identity,
            stageDecision.Stage,
            stageDecision.Condition,
            stageDecision.NextTask,
            candidates,
            handover,
            placement,
            timeline,
            Array.Empty<JourneyAttentionFlag>(),
            audit,
            integrity.Issues,
            JourneyProjectionVersions.Release1);
    }

    private static StageDecision DecideStage(
        JourneyNormalizedFacts facts,
        JourneyIntegrityResult integrity,
        AdmissionJourneyFact? admission)
    {
        // 1. Contradictions / integrity failures
        if (integrity.HasBlockingIssues)
            return Reconciliation(facts, admission, integrity);

        // 2. Authoritative cancellation
        if (admission?.Status == AdmissionStatusEnum.Cancelled)
            return Cancelled("Admisi telah dibatalkan.", "Episode Rawat Inap dibatalkan secara otoritatif.", admission.AdmissionAt);

        if (facts.OpnameRequest?.Status == OpnameRequestStatusEnum.Cancelled && admission is null)
            return Cancelled(
                "Permintaan opname dibatalkan.",
                "Journey prospektif Opname Request dibatalkan sebelum registrasi.",
                facts.OpnameRequest.StatusAt);

        if (facts.Reservation?.Status == ReservationStatusEnum.Cancelled && admission is null)
            return Cancelled(
                "Reservasi dibatalkan.",
                "Journey prospektif Reservasi dibatalkan sebelum registrasi.",
                facts.Reservation.StatusAt);

        // 3. Completion — intentionally unreachable in Release 1.
        // Admission.Completed is flagged by integrity (no authoritative owning process).

        var activeWl = facts.WaitingLists
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.StatusAt)
            .FirstOrDefault();
        var hasClosedHandover = facts.WaitingLists.Any(w => w.Status == WaitingListStatusEnum.Closed);

        // 4. Active Waiting List: Waiting before Accepted
        if (activeWl is { Status: WaitingListStatusEnum.Waiting })
        {
            return new StageDecision(
                JourneyOperationalStage.WardAcceptanceRequired,
                new JourneyCurrentCondition(
                    "Menunggu penerimaan bangsal",
                    "Handover akomodasi telah dibuat dan menunggu respons bangsal tujuan.",
                    activeWl.StatusAt),
                WardTask(
                    JourneyActionCode.WardAcceptHandover,
                    "Terima handover akomodasi",
                    activeWl.BangsalId,
                    activeWl.BangsalName,
                    "Tugas ini merupakan tanggung jawab bangsal tujuan dan tidak dapat dijalankan dari Admisi."));
        }

        if (activeWl is { Status: WaitingListStatusEnum.Accepted })
        {
            return new StageDecision(
                JourneyOperationalStage.HandoverAccepted,
                new JourneyCurrentCondition(
                    "Handover akomodasi diterima",
                    "Bangsal tujuan telah menerima handover; status kamar dan tempat tidur belum tersedia di sistem.",
                    activeWl.StatusAt),
                WardTask(
                    JourneyActionCode.AwaitWardPlacement,
                    "Lanjutkan proses penempatan",
                    activeWl.BangsalId,
                    activeWl.BangsalName,
                    "Penempatan kamar/tempat tidur merupakan tanggung jawab bangsal dan belum tersedia di Admisi."));
        }

        // 5. Closed handover without authoritative placement
        if (hasClosedHandover && admission is not null && IsActiveAdmission(admission.Status))
        {
            var closedAt = facts.WaitingLists
                .Where(w => w.Status == WaitingListStatusEnum.Closed)
                .Select(w => w.StatusAt)
                .Max();
            var ward = facts.WaitingLists
                .Where(w => w.Status == WaitingListStatusEnum.Closed)
                .OrderByDescending(w => w.StatusAt)
                .FirstOrDefault();

            return new StageDecision(
                JourneyOperationalStage.PlacementStatusUnavailable,
                new JourneyCurrentCondition(
                    "Status penempatan tidak tersedia",
                    "Handover akomodasi telah ditutup, tetapi sistem tidak dapat melaporkan kamar, tempat tidur, hunian, atau pelepasan.",
                    closedAt),
                WardTask(
                    JourneyActionCode.AwaitPlacementVisibility,
                    "Tindak lanjut penempatan di luar sistem saat ini",
                    ward?.BangsalId ?? admission.BangsalId,
                    ward?.BangsalName ?? admission.BangsalName,
                    "Status kamar dan tempat tidur belum tersedia; Admisi tidak dapat menjalankan aksi penempatan."));
        }

        // 6. Active Admission without active handover (including WL Cancelled restart)
        if (admission is not null && IsActiveAdmission(admission.Status))
        {
            return new StageDecision(
                JourneyOperationalStage.HandoverRequired,
                new JourneyCurrentCondition(
                    "Handover akomodasi diperlukan",
                    "Registrasi sudah ada dan handover akomodasi harus dibuat atau dibuat ulang.",
                    admission.AdmissionAt),
                AdmisiTask(
                    JourneyActionCode.CreateAccommodationHandover,
                    "Buat handover akomodasi",
                    requiredPermission: null));
        }

        // 7. Unregistered prospective source
        if (admission is null
            && facts.OpnameRequest is { Status: OpnameRequestStatusEnum.Requested })
        {
            return new StageDecision(
                JourneyOperationalStage.RegistrationRequired,
                new JourneyCurrentCondition(
                    "Registrasi diperlukan",
                    "Permintaan opname siap diproses menjadi admisi Rawat Inap.",
                    facts.OpnameRequest.StatusAt),
                AdmisiTask(
                    JourneyActionCode.CompleteRegistration,
                    "Selesaikan registrasi",
                    requiredPermission: null));
        }

        if (admission is null
            && facts.Reservation is
            {
                Status: ReservationStatusEnum.Reserved or ReservationStatusEnum.Maintained
            })
        {
            return new StageDecision(
                JourneyOperationalStage.RegistrationRequired,
                new JourneyCurrentCondition(
                    "Registrasi diperlukan",
                    "Reservasi siap diproses menjadi admisi Rawat Inap.",
                    facts.Reservation.StatusAt),
                AdmisiTask(
                    JourneyActionCode.CompleteRegistration,
                    "Selesaikan registrasi",
                    requiredPermission: null));
        }

        return new StageDecision(
            JourneyOperationalStage.NeedsReconciliation,
            new JourneyCurrentCondition(
                "Perlu rekonsiliasi data",
                "Kombinasi fakta Release 1 tidak dapat dipetakan ke satu tahap operasional yang aman.",
                null),
            SupportTask());
    }

    private static StageDecision Reconciliation(
        JourneyNormalizedFacts facts,
        AdmissionJourneyFact? admission,
        JourneyIntegrityResult integrity)
    {
        var code = integrity.Issues.FirstOrDefault()?.Code
                   ?? JourneyReconciliationCodes.InternallyContradictoryState;
        return new StageDecision(
            JourneyOperationalStage.NeedsReconciliation,
            new JourneyCurrentCondition(
                "Perlu rekonsiliasi data",
                $"Otoritas Release 1 tidak dapat menentukan tahap operasional secara aman ({code}).",
                admission?.AdmissionAt ?? facts.OpnameRequest?.StatusAt ?? facts.Reservation?.StatusAt),
            SupportTask());
    }

    private static StageDecision Cancelled(string title, string explanation, DateTime? since) =>
        new(
            JourneyOperationalStage.Cancelled,
            new JourneyCurrentCondition(title, explanation, since),
            null);

    private static JourneyNextTask AdmisiTask(
        JourneyActionCode code,
        string label,
        string? requiredPermission) =>
        new(
            code,
            label,
            new JourneyTaskOwner(JourneyOwnerDomain.Admisi, null, "Admisi"),
            CanExecute: true,
            BlockedReason: null,
            RequiredPermission: requiredPermission);

    private static JourneyNextTask WardTask(
        JourneyActionCode code,
        string label,
        string? unitId,
        string? unitName,
        string blockedReason) =>
        new(
            code,
            label,
            new JourneyTaskOwner(JourneyOwnerDomain.Ward, unitId, unitName ?? "Bangsal"),
            CanExecute: false,
            BlockedReason: blockedReason,
            RequiredPermission: null);

    private static JourneyNextTask SupportTask() =>
        new(
            JourneyActionCode.InvestigateReconciliation,
            "Selidiki isu koordinasi/data",
            new JourneyTaskOwner(JourneyOwnerDomain.SystemSupport, null, "Dukungan sistem"),
            CanExecute: false,
            BlockedReason: "Status rekonsiliasi bersifat baca-saja dari batas Admisi.",
            RequiredPermission: null);

    /// <summary>
    /// Stage/ownership candidates only. Final CancelAdmission executability requires Tata Rekening
    /// billing evaluation in the projection layer (<see cref="JourneyAllowedActionEvaluator"/>).
    /// </summary>
    private static IReadOnlyList<JourneyAllowedAction> BuildCandidateActions(
        JourneyNormalizedFacts facts,
        AdmissionJourneyFact? admission,
        StageDecision decision)
    {
        var actions = new List<JourneyAllowedAction>();

        if (decision.Stage is JourneyOperationalStage.NeedsReconciliation
            or JourneyOperationalStage.Cancelled
            or JourneyOperationalStage.Completed)
        {
            return actions;
        }

        if (decision.NextTask is { } next)
        {
            actions.Add(new JourneyAllowedAction(
                next.Code,
                next.Label,
                next.CanExecute,
                next.BlockedReason,
                next.RequiredPermission));
        }

        // Independent Admisi-owned actions when domain state permits (boundary only; no auth policy).
        if (decision.Stage == JourneyOperationalStage.RegistrationRequired)
        {
            if (facts.OpnameRequest?.Status == OpnameRequestStatusEnum.Requested)
            {
                actions.Add(Executable(
                    JourneyActionCode.CancelOpnameRequest,
                    "Batalkan permintaan opname"));
            }

            if (facts.Reservation?.Status is ReservationStatusEnum.Reserved
                or ReservationStatusEnum.Maintained)
            {
                actions.Add(Executable(
                    JourneyActionCode.CancelReservation,
                    "Batalkan reservasi"));
            }
        }

        if (admission is not null && IsActiveAdmission(admission.Status)
            && decision.Stage is not JourneyOperationalStage.NeedsReconciliation)
        {
            if (decision.Stage == JourneyOperationalStage.HandoverRequired)
            {
                actions.Add(Executable(
                    JourneyActionCode.UpdateAdmission,
                    "Perbarui admisi"));
            }

            actions.Add(Executable(
                JourneyActionCode.CancelAdmission,
                "Batalkan admisi"));
        }

        return actions
            .GroupBy(a => a.Code)
            .Select(g => g.First())
            .ToList();
    }

    private static JourneyAllowedAction Executable(JourneyActionCode code, string label) =>
        new(code, label, CanExecute: true, BlockedReason: null, RequiredPermission: null);

    private static JourneyAccommodationHandoverSummary BuildHandoverSummary(
        JourneyNormalizedFacts facts)
    {
        // Prefer a single active handover; if integrity already flagged multiples, still report one
        // without throwing so the reconciliation workspace remains readable.
        var active = facts.WaitingLists
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.StatusAt)
            .FirstOrDefault();
        return new JourneyAccommodationHandoverSummary(
            active?.WaitingListId,
            active?.Status,
            active?.BangsalId,
            active?.BangsalName,
            facts.WaitingLists.Count,
            facts.WaitingLists.Count(w => w.Status == WaitingListStatusEnum.Closed),
            facts.WaitingLists.Count(w => w.Status == WaitingListStatusEnum.Cancelled));
    }

    private static JourneyPlacementAvailability BuildPlacementAvailability(
        JourneyNormalizedFacts facts,
        AdmissionJourneyFact? admission,
        JourneyOperationalStage stage)
    {
        var wardId = facts.WaitingLists
                         .OrderByDescending(w => w.IsActive)
                         .ThenByDescending(w => w.StatusAt)
                         .Select(w => w.BangsalId)
                         .FirstOrDefault(JourneyIdFactory.HasValue)
                     ?? admission?.BangsalId;
        var wardName = facts.WaitingLists
                           .OrderByDescending(w => w.IsActive)
                           .ThenByDescending(w => w.StatusAt)
                           .Select(w => w.BangsalName)
                           .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                       ?? admission?.BangsalName;

        JourneyTaskOwner? ward = JourneyIdFactory.HasValue(wardId)
            ? new JourneyTaskOwner(JourneyOwnerDomain.Ward, wardId, wardName ?? "Bangsal")
            : null;

        var explanation = stage == JourneyOperationalStage.PlacementStatusUnavailable
            ? "Handover akomodasi telah ditutup, tetapi status kamar dan tempat tidur belum tersedia di sistem."
            : "Status kamar dan tempat tidur belum tersedia di sistem (Ward Management belum diimplementasikan).";

        return new JourneyPlacementAvailability(
            JourneyPlacementAvailabilityStatus.Unavailable,
            JourneyPlacementUnavailableReasonCode.WardManagementNotImplemented,
            ward,
            explanation);
    }

    private static IReadOnlyList<JourneyTimelineEvent> BuildTimeline(JourneyNormalizedFacts facts)
    {
        var events = new List<JourneyTimelineEvent>();

        if (facts.OpnameRequest is { } opn)
        {
            events.Add(new JourneyTimelineEvent(
                JourneyTimelineEventKind.OpnameRequested,
                opn.StatusAt,
                opn.OpnameRequestId,
                "Opname Request dibuat"));
            if (opn.Status == OpnameRequestStatusEnum.Cancelled)
                events.Add(new JourneyTimelineEvent(
                    JourneyTimelineEventKind.OpnameCancelled,
                    opn.StatusAt,
                    opn.OpnameRequestId,
                    "Opname Request dibatalkan"));
            if (opn.Status == OpnameRequestStatusEnum.Fulfilled)
                events.Add(new JourneyTimelineEvent(
                    JourneyTimelineEventKind.OpnameFulfilled,
                    opn.StatusAt,
                    opn.OpnameRequestId,
                    "Opname Request dipenuhi"));
        }

        if (facts.Reservation is { } rsv)
        {
            events.Add(new JourneyTimelineEvent(
                JourneyTimelineEventKind.ReservationCreated,
                rsv.StatusAt,
                rsv.ReservationId,
                "Reservasi dibuat"));
            if (rsv.Status == ReservationStatusEnum.Maintained)
                events.Add(new JourneyTimelineEvent(
                    JourneyTimelineEventKind.ReservationMaintained,
                    rsv.StatusAt,
                    rsv.ReservationId,
                    "Reservasi dipelihara"));
            if (rsv.Status == ReservationStatusEnum.Cancelled)
                events.Add(new JourneyTimelineEvent(
                    JourneyTimelineEventKind.ReservationCancelled,
                    rsv.StatusAt,
                    rsv.ReservationId,
                    "Reservasi dibatalkan"));
            if (rsv.Status == ReservationStatusEnum.Realized)
                events.Add(new JourneyTimelineEvent(
                    JourneyTimelineEventKind.ReservationRealized,
                    rsv.StatusAt,
                    rsv.ReservationId,
                    "Reservasi direalisasikan"));
        }

        foreach (var admission in facts.Admissions)
        {
            events.Add(new JourneyTimelineEvent(
                JourneyTimelineEventKind.AdmissionRegistered,
                admission.AdmissionAt,
                admission.RegId,
                "Admisi didaftarkan"));
            if (admission.Status == AdmissionStatusEnum.Updated)
                events.Add(new JourneyTimelineEvent(
                    JourneyTimelineEventKind.AdmissionUpdated,
                    admission.AdmissionAt,
                    admission.RegId,
                    "Admisi diperbarui"));
            if (admission.Status == AdmissionStatusEnum.Cancelled)
                events.Add(new JourneyTimelineEvent(
                    JourneyTimelineEventKind.AdmissionCancelled,
                    admission.AdmissionAt,
                    admission.RegId,
                    "Admisi dibatalkan"));
        }

        foreach (var wl in facts.WaitingLists.OrderBy(w => w.StatusAt))
        {
            var kind = wl.Status switch
            {
                WaitingListStatusEnum.Waiting => JourneyTimelineEventKind.WaitingListCreated,
                WaitingListStatusEnum.Accepted => JourneyTimelineEventKind.WaitingListAccepted,
                WaitingListStatusEnum.Closed => JourneyTimelineEventKind.WaitingListClosed,
                WaitingListStatusEnum.Cancelled => JourneyTimelineEventKind.WaitingListCancelled,
                _ => JourneyTimelineEventKind.WaitingListCreated
            };
            events.Add(new JourneyTimelineEvent(
                kind,
                wl.StatusAt,
                wl.WaitingListId,
                $"Waiting List {wl.Status}"));
        }

        return events;
    }

    private static JourneyIdentity BuildIdentity(
        string journeyId,
        JourneyNormalizedFacts facts,
        AdmissionJourneyFact? admission,
        JourneyOperationalStage stage)
    {
        var isTerminal = stage is JourneyOperationalStage.Cancelled
            or JourneyOperationalStage.Completed;
        var isRegistered = admission is not null;
        return new JourneyIdentity(
            journeyId,
            admission?.RegId,
            facts.OriginKind,
            IsProspective: !isRegistered,
            IsRegistered: isRegistered,
            IsTerminal: isTerminal);
    }

    private static JourneySystemAuditReferences BuildAudit(
        JourneyNormalizedFacts facts,
        AdmissionJourneyFact? admission) =>
        new(
            facts.OriginKind,
            facts.OpnameRequest?.OpnameRequestId,
            facts.OpnameRequest?.Status,
            facts.Reservation?.ReservationId,
            facts.Reservation?.Status,
            admission?.RegId,
            admission?.Status,
            facts.WaitingLists.Select(w => w.WaitingListId).ToArray(),
            DateTime.UtcNow);

    private static bool IsActiveAdmission(AdmissionStatusEnum status) =>
        status is AdmissionStatusEnum.Admitted
            or AdmissionStatusEnum.Updated
            or AdmissionStatusEnum.Waiting;

    private sealed record StageDecision(
        JourneyOperationalStage Stage,
        JourneyCurrentCondition Condition,
        JourneyNextTask? NextTask);
}
