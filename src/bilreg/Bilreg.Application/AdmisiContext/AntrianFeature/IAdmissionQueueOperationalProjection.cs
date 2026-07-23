namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public enum AdmissionQueueCreationReason
{
    Normal = 0,
    Redirected = 1,
    ManualPriority = 2
}

public enum AdmissionQueueClaimState
{
    Released = 0,
    Outstanding = 1,
    InService = 2
}

public sealed record AdmissionQueueWorklistFilter(
    DateOnly BusinessDate,
    string? ServicePointId = null,
    int? QueueStatus = null,
    string? LoketKey = null,
    int Offset = 0,
    int Limit = 100);

public sealed record AdmissionQueueWorklistItem(
    string AntrianId,
    int NoUrut,
    string? QueueLabel,
    string ServicePointId,
    string ServicePointName,
    int QueueStatus,
    bool Priority,
    AdmissionQueueCreationReason CreationReason,
    int CallCount,
    string? LoketKey,
    AdmissionQueueClaimState? ClaimState,
    DateTime CreatedAt,
    DateTime? ServedAt,
    DateTime? DoneAt,
    string? PasienTrackerId);

public sealed record CurrentLoketDisplayItem(
    string LoketKey,
    string AntrianId,
    int NoUrut,
    string? QueueLabel,
    string ServicePointId,
    AdmissionQueueClaimState DisplayState,
    long AnnouncementVersion,
    DateTime CalledAt,
    DateTime? ServiceStartedAt,
    byte[] RowVersion);

public interface IAdmissionQueueOperationalProjection
{
    IReadOnlyList<AdmissionQueueWorklistItem> ListWorklist(AdmissionQueueWorklistFilter filter);
    IReadOnlyList<CurrentLoketDisplayItem> ListCurrentLoket(string? loketKey = null);
}

public static class QueueAnnouncementPolicy
{
    public static bool ShouldPlay(long reloadedVersion, long lastProcessedVersion) =>
        reloadedVersion > lastProcessedVersion;
}

public static class AdmissionQueueWorklistOrdering
{
    public static IOrderedEnumerable<AdmissionQueueWorklistItem> Apply(
        IEnumerable<AdmissionQueueWorklistItem> items) => items
        .OrderByDescending(x => x.Priority)
        .ThenBy(x => x.CreatedAt)
        .ThenBy(x => x.NoUrut);
}
