using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Shared admission identification: AssignPasien + Serve + Check In / Reg-Start evidence (workflow 10.3).
/// </summary>
internal static class AdmissionQueueIdentify
{
    public const string CheckInEventName = "Check In";
    public const string RegStartEventName = "Reg-Start";

    public static AntrianEntryModel RequireAnonymousWaitingEntry(
        AntrianModel queue,
        int noUrut)
    {
        var entry = queue.ListEntry.FirstOrDefault(x => x.NoUrut == noUrut)
            ?? throw new KeyNotFoundException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} not found");

        if (PasienTrackerStableIdentity.IsRealTrackerId(entry.Tracker.PasienTrackerId))
            throw new InvalidOperationException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} is already identified.");

        if (entry.AntrianStatus != AntrianStatusEnum.Waiting)
            throw new InvalidOperationException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} is not Waiting.");

        return entry;
    }

    public static string IdentifyAndRecordEvidence(
        AntrianModel queue,
        AntrianEntryModel entry,
        PasienTrackerModel tracker,
        DateTime servedAt)
    {
        var queueRef = QueueEvidenceReference.Create(queue.AntrianId, entry.NoUrut).Value;
        var checkInAt = entry.CreatedAt;

        entry.AssignPasien(tracker);
        entry.Serve(servedAt);

        // Select path: append both events. New path: Create already recorded Check In.
        if (!tracker.ListEvent.Any(e =>
                e.EventName == CheckInEventName && e.ReffId == queueRef))
        {
            tracker.AddEvent(CheckInEventName, queueRef, checkInAt);
        }

        tracker.AddEvent(RegStartEventName, queueRef, servedAt);
        return queueRef;
    }
}
