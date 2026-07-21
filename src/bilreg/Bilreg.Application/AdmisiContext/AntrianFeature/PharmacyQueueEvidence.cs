using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Appends pharmacy service evidence to Patient Tracker (workflow 10.6 / F-09).
/// </summary>
internal static class PharmacyQueueEvidence
{
    public const string ApotekStartEventName = "Apotek-Start";
    public const string ApotekDoneEventName = "Apotek-Done";

    public static PasienTrackerModel RequireTracker(
        IPasienTrackerRepo trackerRepo,
        string pasienTrackerId)
    {
        if (!PasienTrackerStableIdentity.IsRealTrackerId(pasienTrackerId))
            throw new InvalidOperationException(
                "Queue entry is not identified with a Patient Tracker.");

        return trackerRepo.LoadEntity(PasienTrackerModel.Key(pasienTrackerId))
            .GetValueOrThrow(
                $"PasienTracker '{pasienTrackerId}' not found");
    }

    public static void AppendApotekStart(
        PasienTrackerModel tracker,
        string reffId,
        DateTime servedAt)
    {
        if (tracker.ListEvent.Any(e =>
                e.EventName == ApotekStartEventName && e.ReffId == reffId))
            return;

        tracker.AddEvent(ApotekStartEventName, reffId, servedAt);
    }

    public static void AppendApotekDone(
        PasienTrackerModel tracker,
        string reffId,
        DateTime doneAt)
    {
        if (tracker.ListEvent.Any(e =>
                e.EventName == ApotekDoneEventName && e.ReffId == reffId))
            return;

        tracker.AddEvent(ApotekDoneEventName, reffId, doneAt);
    }

    public static void Append(
        PasienTrackerModel tracker,
        string eventName,
        string reffId,
        DateTime occurredAt)
    {
        switch (eventName)
        {
            case ApotekStartEventName:
                AppendApotekStart(tracker, reffId, occurredAt);
                break;
            case ApotekDoneEventName:
                AppendApotekDone(tracker, reffId, occurredAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(eventName),
                    $"Unsupported pharmacy tracker event '{eventName}'.");
        }
    }
}
