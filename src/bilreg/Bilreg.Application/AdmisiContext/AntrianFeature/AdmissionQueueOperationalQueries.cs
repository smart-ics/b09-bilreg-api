using MediatR;

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

public record AdmissionQueueOfficerWorklistQuery(
    string BusinessDateYmd,
    string? ServicePointId = null,
    int? QueueStatus = null,
    string? LoketKey = null,
    int Offset = 0,
    int Limit = 100) : IRequest<IReadOnlyList<AdmissionQueueWorklistItem>>;

public sealed class AdmissionQueueOfficerWorklistHandler
    : IRequestHandler<AdmissionQueueOfficerWorklistQuery, IReadOnlyList<AdmissionQueueWorklistItem>>
{
    private readonly IAdmissionQueueOperationalProjection _projection;
    public AdmissionQueueOfficerWorklistHandler(IAdmissionQueueOperationalProjection projection) =>
        _projection = projection;

    public Task<IReadOnlyList<AdmissionQueueWorklistItem>> Handle(
        AdmissionQueueOfficerWorklistQuery request, CancellationToken cancellationToken)
    {
        var date = DateOnly.ParseExact(request.BusinessDateYmd, "yyyy-MM-dd");
        if (request.Offset < 0) throw new ArgumentOutOfRangeException(nameof(request.Offset));
        if (request.Limit is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(request.Limit));
        var filter = new AdmissionQueueWorklistFilter(date, request.ServicePointId?.Trim(),
            request.QueueStatus, request.LoketKey?.Trim(), request.Offset, request.Limit);
        return Task.FromResult(_projection.ListWorklist(filter));
    }
}

public record CurrentLoketDisplaySnapshotQuery(string? LoketKey = null)
    : IRequest<IReadOnlyList<CurrentLoketDisplayItem>>;

public sealed class CurrentLoketDisplaySnapshotHandler
    : IRequestHandler<CurrentLoketDisplaySnapshotQuery, IReadOnlyList<CurrentLoketDisplayItem>>
{
    private readonly IAdmissionQueueOperationalProjection _projection;
    public CurrentLoketDisplaySnapshotHandler(IAdmissionQueueOperationalProjection projection) =>
        _projection = projection;

    public Task<IReadOnlyList<CurrentLoketDisplayItem>> Handle(
        CurrentLoketDisplaySnapshotQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_projection.ListCurrentLoket(request.LoketKey?.Trim()));
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
