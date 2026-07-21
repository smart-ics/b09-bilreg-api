namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public sealed class QueueNumberCompatibilityOptions
{
    public const string SectionName = "QueueNumber";

    public QueueNumberAuthority Authority { get; set; } = QueueNumberAuthority.LegacyMap;

    /// <summary>Reserved for future cutover; not enforced in V1.</summary>
    public DateTime? CutoverAt { get; set; }
}
