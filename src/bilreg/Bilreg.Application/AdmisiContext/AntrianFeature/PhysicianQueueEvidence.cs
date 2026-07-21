using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Appends physician consultation evidence to Patient Tracker (workflow 10.4 / F-08).
/// </summary>
internal static class PhysicianQueueEvidence
{
    public const string ConsultStartEventName = "Consult-Start";
    public const string ConsultDoneEventName = "Consult-Done";

    public static string QueueRef(AntrianModel queue, AntrianEntryModel entry)
        => QueueEvidenceReference.Create(queue.AntrianId, entry.NoUrut).Value;

    public static PasienTrackerModel RequireTracker(
        IPasienTrackerRepo trackerRepo,
        AntrianEntryModel entry)
    {
        if (!PasienTrackerStableIdentity.IsRealTrackerId(entry.Tracker.PasienTrackerId))
            throw new InvalidOperationException(
                "Queue entry is not identified with a Patient Tracker.");

        return trackerRepo.LoadEntity(entry.Tracker)
            .GetValueOrThrow(
                $"PasienTracker '{entry.Tracker.PasienTrackerId}' not found");
    }

    public static void AppendConsultStart(
        PasienTrackerModel tracker,
        string queueRef,
        DateTime servedAt)
    {
        if (tracker.ListEvent.Any(e =>
                e.EventName == ConsultStartEventName && e.ReffId == queueRef))
            return;

        tracker.AddEvent(ConsultStartEventName, queueRef, servedAt);
    }

    public static void AppendConsultDone(
        PasienTrackerModel tracker,
        string queueRef,
        DateTime doneAt)
    {
        if (tracker.ListEvent.Any(e =>
                e.EventName == ConsultDoneEventName && e.ReffId == queueRef))
            return;

        tracker.AddEvent(ConsultDoneEventName, queueRef, doneAt);
    }
}
