namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Raised when a concurrent first daily-session insert loses the SequenceTag unique race.
/// Ordinary intake reloads the winner session and continues allocation.
/// </summary>
public sealed class AdmissionQueueSessionRaceException : InvalidOperationException
{
    public string SequenceTag { get; }

    public AdmissionQueueSessionRaceException(string sequenceTag, Exception? inner = null)
        : base($"Concurrent daily queue session create lost unique SequenceTag race for '{sequenceTag}'.", inner)
    {
        SequenceTag = sequenceTag;
    }
}
