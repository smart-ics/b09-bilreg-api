using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Completes the admission Queue Entry when Registration finishes (F-07 / BR-TRK-041).
/// Physician ServedAt must not be set here — legacy "active at Reg" uses ReffDesc=REG / AntrianMap, not physician InService.
/// </summary>
internal static class AdmissionQueueComplete
{
    public const string RegisterEventName = "REGISTER";
    public const string DefaultServicePointCode = "ADM";
    public const string DefaultServicePointName = "Loket Admisi";

    /// <summary>
    /// When admission keys are supplied, Done the InService entry.
    /// Otherwise create-on-reg: identified Waiting → Serve → Done in one step (legacy callers without intake).
    /// </summary>
    public static AntrianModel CompleteAtRegistration(
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        PasienTrackerModel tracker,
        string regId,
        DateTime occurredAt,
        string? admissionAntrianId,
        int? admissionNoUrut,
        ServicePointType admissionServicePoint,
        IAdmissionServicePointResolver servicePointResolver)
    {
        if (!string.IsNullOrWhiteSpace(admissionAntrianId) && admissionNoUrut is > 0)
        {
            var queue = antrianRepo.LoadEntity(AntrianModel.Key(admissionAntrianId!))
                .GetValueOrThrow($"Admission queue '{admissionAntrianId}' not found");
            servicePointResolver.EnsureAdmissionQueue(queue);
            var entry = RequireIdentifiedInServiceEntry(queue, admissionNoUrut.Value, tracker);
            CompleteInServiceEntry(entry, tracker, regId, occurredAt);
            return queue;
        }

        var admissionQueue = ResolveOrCreateAdmissionSession(
            antrianRepo, antrianFactory, occurredAt, admissionServicePoint);
        CreateServeAndComplete(admissionQueue, tracker, regId, occurredAt);
        return admissionQueue;
    }

    public static AntrianEntryModel RequireIdentifiedInServiceEntry(
        AntrianModel queue,
        int noUrut,
        PasienTrackerModel tracker)
    {
        var entry = queue.ListEntry.FirstOrDefault(x => x.NoUrut == noUrut)
            ?? throw new KeyNotFoundException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} not found");

        if (!PasienTrackerStableIdentity.IsRealTrackerId(entry.Tracker.PasienTrackerId))
            throw new InvalidOperationException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} is not identified.");

        if (entry.Tracker.PasienTrackerId != tracker.PasienTrackerId)
            throw new InvalidOperationException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} belongs to a different Tracker.");

        if (entry.AntrianStatus != AntrianStatusEnum.InService)
            throw new InvalidOperationException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} is not In Service.");

        return entry;
    }

    public static void CompleteInServiceEntry(
        AntrianEntryModel entry,
        PasienTrackerModel tracker,
        string regId,
        DateTime doneAt)
    {
        entry.Done(doneAt);
        AppendRegisterIfMissing(tracker, regId, doneAt);
    }

    public static AntrianEntryModel AttachNewTrackerAndComplete(
        AntrianModel queue,
        int noUrut,
        PasienTrackerModel tracker,
        string regId,
        DateTime doneAt)
    {
        var entry = AdmissionQueueIdentify.RequireAnonymousInServiceEntry(queue, noUrut);
        entry.AssignPasien(tracker);
        CompleteInServiceEntry(entry, tracker, regId, doneAt);
        return entry;
    }

    public static AntrianEntryModel CreateServeAndComplete(
        AntrianModel queue,
        PasienTrackerModel tracker,
        string regId,
        DateTime occurredAt)
    {
        var entry = queue.AddEntry(tracker, occurredAt);
        entry.Serve(occurredAt);
        entry.Done(occurredAt);
        AppendRegisterIfMissing(tracker, regId, occurredAt);
        return entry;
    }

    public static AntrianModel ResolveOrCreateAdmissionSession(
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        DateTime occurredAt,
        ServicePointType servicePoint)
    {
        var businessDate = DateOnly.FromDateTime(occurredAt);
        var sequenceTag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue, servicePoint);

        var listQue = antrianRepo.ListData(businessDate) ?? [];
        var queView = listQue.FirstOrDefault(x => x.SequenceTag == sequenceTag);
        return queView is null
            ? antrianFactory.Create(servicePoint, businessDate)
            : antrianRepo.LoadEntity(queView).Value;
    }

    public static void AppendRegisterIfMissing(
        PasienTrackerModel tracker,
        string regId,
        DateTime occurredAt)
    {
        if (tracker.ListEvent.Any(e =>
                e.EventName == RegisterEventName && e.ReffId == regId))
            return;

        tracker.AddEvent(RegisterEventName, regId, occurredAt);
    }
}
