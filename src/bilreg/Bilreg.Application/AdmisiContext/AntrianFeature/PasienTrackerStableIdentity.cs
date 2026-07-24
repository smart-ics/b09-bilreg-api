using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Keeps TrackerId stable across visit change (BR-TRK-009a/b).
/// </summary>
public static class PasienTrackerStableIdentity
{
    public static bool IsRealTrackerId(string? trackerId)
        => trackerId is not null
           && trackerId.Trim() != ""
           && trackerId != "-";

    public static PasienTrackerModel ForVisitChange(
        PasienTrackerModel? existingTracker,
        RegModel reg,
        DateTime occurredAt)
    {
        if (existingTracker is not null)
        {
            existingTracker.AddEvent("VISIT_CHANGED", reg.RegId, occurredAt);
            return existingTracker;
        }

        var created = PasienTrackerModel.Create(reg, occurredAt);
        created.AddEvent("VISIT_CHANGED", reg.RegId, occurredAt);
        return created;
    }
}
