using Ardalis.GuardClauses;
using MediatR;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record AdmissionQueueOfficerWorklistQry(
    string BusinessDateYmd,
    string? ServicePointId = null,
    int? QueueStatus = null,
    string? LoketKey = null,
    int Offset = 0,
    int Limit = 100) : IRequest<IReadOnlyList<AdmissionQueueWorklistItem>>;

public sealed class AdmissionQueueOfficerWorklistHandler
    : IRequestHandler<AdmissionQueueOfficerWorklistQry, IReadOnlyList<AdmissionQueueWorklistItem>>
{
    private readonly IAdmissionQueueOperationalProjection _projection;

    public AdmissionQueueOfficerWorklistHandler(IAdmissionQueueOperationalProjection projection)
    {
        _projection = projection;
    }

    public Task<IReadOnlyList<AdmissionQueueWorklistItem>> Handle(
        AdmissionQueueOfficerWorklistQry request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BusinessDateYmd);
        if (request.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Offset));
        if (request.Limit is < 1 or > 500)
            throw new ArgumentOutOfRangeException(nameof(request.Limit));

        var businessDate = DateOnly.ParseExact(request.BusinessDateYmd, "yyyy-MM-dd");
        var filter = new AdmissionQueueWorklistFilter(
            businessDate,
            request.ServicePointId?.Trim(),
            request.QueueStatus,
            request.LoketKey?.Trim(),
            request.Offset,
            request.Limit);

        return Task.FromResult(_projection.ListWorklist(filter));
    }
}

public record CurrentLoketDisplaySnapshotQry(string? LoketKey = null)
    : IRequest<IReadOnlyList<CurrentLoketDisplayItem>>;

public sealed class CurrentLoketDisplaySnapshotHandler
    : IRequestHandler<CurrentLoketDisplaySnapshotQry, IReadOnlyList<CurrentLoketDisplayItem>>
{
    private readonly IAdmissionQueueOperationalProjection _projection;

    public CurrentLoketDisplaySnapshotHandler(IAdmissionQueueOperationalProjection projection)
    {
        _projection = projection;
    }

    public Task<IReadOnlyList<CurrentLoketDisplayItem>> Handle(
        CurrentLoketDisplaySnapshotQry request,
        CancellationToken cancellationToken)
    {
        var loketKey = request.LoketKey?.Trim();
        return Task.FromResult(_projection.ListCurrentLoket(loketKey));
    }
}
