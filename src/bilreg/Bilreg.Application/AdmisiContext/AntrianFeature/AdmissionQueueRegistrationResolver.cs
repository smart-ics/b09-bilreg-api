using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

internal record AdmissionQueueRegistrationResolution(
    PasienTrackerModel Tracker,
    AntrianEntryModel Entry,
    bool RequiresConditionalSave);

internal static class AdmissionQueueRegistrationResolver
{
    public static AdmissionQueueRegistrationResolution ResolveAndComplete(
        IPasienTrackerRepo trackerRepo,
        AntrianModel queue,
        int noUrut,
        RegModel reg,
        DateTime occurredAt)
    {
        var entry = AdmissionQueueIdentify.RequireInServiceEntry(queue, noUrut);
        if (PasienTrackerStableIdentity.IsRealTrackerId(entry.Tracker.PasienTrackerId))
        {
            var tracker = trackerRepo.LoadEntity(entry.Tracker)
                .GetValueOrThrow($"PasienTracker '{entry.Tracker.PasienTrackerId}' not found");
            AdmissionQueueComplete.RequireIdentifiedInServiceEntry(queue, noUrut, tracker);
            AdmissionQueueComplete.CompleteInServiceEntry(entry, tracker, reg.RegId, occurredAt);
            return new AdmissionQueueRegistrationResolution(tracker, entry, false);
        }

        var queueRef = QueueEvidenceReference.Create(queue.AntrianId, entry.NoUrut).Value;
        var newTracker = PasienTrackerModel.CreateFromRegistrationWithAdmissionEvidence(
            reg, queueRef, entry.CreatedAt, entry.ServedAt, occurredAt);
        AdmissionQueueComplete.AttachNewTrackerAndComplete(
            queue, entry.NoUrut, newTracker, reg.RegId, occurredAt);
        return new AdmissionQueueRegistrationResolution(newTracker, entry, true);
    }
}
