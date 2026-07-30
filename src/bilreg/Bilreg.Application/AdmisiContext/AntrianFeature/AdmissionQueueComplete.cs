using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Completes the admission Queue Entry when Registration finishes (F-07 / BR-TRK-041).
/// Physician ServedAt must not be set here — legacy "active at Reg" uses ReffDesc=REG / AntrianMap, not physician InService.
/// </summary>
internal static class AdmissionQueueComplete
{
    public const string RegisterEventName = "REGISTER";
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
