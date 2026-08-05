namespace Farinv.Domain.SalesContext.AntrianFeature;

public static class PharmacyTrackerIdentity
{
    public static bool IsRealTrackerId(string? trackerId)
        => trackerId is not null
           && trackerId.Trim() != ""
           && trackerId != "-";

    public static void EnsureRealTrackerId(string? trackerId)
    {
        if (!IsRealTrackerId(trackerId))
            throw new InvalidOperationException("Queue entry is not identified with a Patient Tracker.");
    }
}
