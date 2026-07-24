using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Shared admission identification and evidence behaviour (workflow 10.3).
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

    public static AntrianEntryModel RequireAnonymousInServiceEntry(
        AntrianModel queue,
        int noUrut)
    {
        var entry = queue.ListEntry.FirstOrDefault(x => x.NoUrut == noUrut)
            ?? throw new KeyNotFoundException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} not found");

        if (PasienTrackerStableIdentity.IsRealTrackerId(entry.Tracker.PasienTrackerId))
            throw new InvalidOperationException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} is already identified.");

        if (entry.AntrianStatus != AntrianStatusEnum.InService)
            throw new InvalidOperationException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} is not In Service.");

        return entry;
    }

    public static AntrianEntryModel RequireInServiceEntry(
        AntrianModel queue,
        int noUrut)
    {
        var entry = queue.ListEntry.FirstOrDefault(x => x.NoUrut == noUrut)
            ?? throw new KeyNotFoundException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} not found");

        if (entry.AntrianStatus != AntrianStatusEnum.InService)
            throw new InvalidOperationException(
                $"Queue entry '{queue.AntrianId}' / {noUrut} is not In Service.");

        return entry;
    }

    public static string IdentifyExistingTrackerAndRecordEvidence(
        AntrianModel queue,
        AntrianEntryModel entry,
        PasienTrackerModel tracker)
    {
        var queueRef = QueueEvidenceReference.Create(queue.AntrianId, entry.NoUrut).Value;
        var checkInAt = entry.CreatedAt;

        entry.AssignPasien(tracker);
        tracker.AddEvent(CheckInEventName, queueRef, checkInAt);
        tracker.AddEvent(RegStartEventName, queueRef, entry.ServedAt);
        return queueRef;
    }
}
