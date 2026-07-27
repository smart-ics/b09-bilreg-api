using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public static class AdmissionQueueClosingDispositions
{
    public const string NoShow = "NoShow";
    public const string Withdraw = "Withdraw";
}

public sealed record AdmissionQueueClosingPreviewItem(
    string AntrianId, int NoUrut, string? QueueLabel, string? PersonName,
    string? ReferenceId, string? ReferenceDescription, int CallCount, DateTime? LastCalledAt,
    AdmissionQueueClaimState? ClaimState, string? ClaimLoketKey, string? ClaimRowVersion,
    IReadOnlyList<string> AllowedDispositions);

public sealed record AdmissionQueueClosingPreviewResponse(
    DateOnly BusinessDate, string ServicePointId, IReadOnlyList<AdmissionQueueClosingPreviewItem> Entries);

public sealed record AdmissionQueueClosingPreviewQry(string BusinessDateYmd, string ServicePointId)
    : IRequest<AdmissionQueueClosingPreviewResponse>;

public sealed record AdmissionQueueClosingDecision(
    string AntrianId, int NoUrut, string Disposition, string? Reason, byte[]? ExpectedClaimRowVersion);

public sealed record AdmissionQueueCloseCmd(
    string BusinessDateYmd, string ServicePointId, string UserId,
    IReadOnlyList<AdmissionQueueClosingDecision> Decisions)
    : IRequest<AdmissionQueueCloseResponse>;

public sealed record AdmissionQueueCloseResponse(
    DateOnly BusinessDate, string ServicePointId, int ReviewedCount, int NoShowCount,
    int WithdrawnCount, DateTime ClosedAt);

public interface IAdmissionQueueClosingRepo
{
    IReadOnlyList<AdmissionQueueClosingPreviewItem> ListWaiting(DateOnly businessDate, string servicePointId);
    AdmissionQueueCloseResponse Close(AdmissionQueueCloseCmd command, DateOnly businessDate, DateTime closedAt);
}

public sealed class AdmissionQueueClosingPreviewHandler
    : IRequestHandler<AdmissionQueueClosingPreviewQry, AdmissionQueueClosingPreviewResponse>
{
    private readonly IAdmissionQueueClosingRepo _repo;
    public AdmissionQueueClosingPreviewHandler(IAdmissionQueueClosingRepo repo) => _repo = repo;

    public Task<AdmissionQueueClosingPreviewResponse> Handle(AdmissionQueueClosingPreviewQry request, CancellationToken cancellationToken)
    {
        var date = Parse(request.BusinessDateYmd);
        var servicePointId = RequireServicePoint(request.ServicePointId);
        return Task.FromResult(new AdmissionQueueClosingPreviewResponse(date, servicePointId, _repo.ListWaiting(date, servicePointId)));
    }

    internal static DateOnly Parse(string value)
    {
        try { return DateOnly.ParseExact(value, "yyyy-MM-dd"); }
        catch (FormatException) { throw new ArgumentException("BusinessDate must use yyyy-MM-dd."); }
    }

    internal static string RequireServicePoint(string value)
    {
        Guard.Against.NullOrWhiteSpace(value);
        return value.Trim();
    }
}

public sealed class AdmissionQueueCloseHandler : IRequestHandler<AdmissionQueueCloseCmd, AdmissionQueueCloseResponse>
{
    private readonly IAdmissionQueueClosingRepo _repo;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionQueueRefreshPublisher _publisher;
    public AdmissionQueueCloseHandler(IAdmissionQueueClosingRepo repo, ITglJamProvider clock, IAdmissionQueueRefreshPublisher publisher)
        => (_repo, _clock, _publisher) = (repo, clock, publisher);

    public async Task<AdmissionQueueCloseResponse> Handle(AdmissionQueueCloseCmd request, CancellationToken cancellationToken)
    {
        var date = AdmissionQueueClosingPreviewHandler.Parse(request.BusinessDateYmd);
        var servicePoint = AdmissionQueueClosingPreviewHandler.RequireServicePoint(request.ServicePointId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.Decisions is null) throw new ArgumentException("Decisions is required.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var decision in request.Decisions)
        {
            Guard.Against.NullOrWhiteSpace(decision.AntrianId);
            if (decision.NoUrut <= 0) throw new ArgumentException("NoUrut must be positive.");
            if (!keys.Add($"{decision.AntrianId}:{decision.NoUrut}")) throw new ArgumentException("Duplicate queue decision.");
            if (decision.Disposition is not (AdmissionQueueClosingDispositions.NoShow or AdmissionQueueClosingDispositions.Withdraw))
                throw new ArgumentException("Disposition must be NoShow or Withdraw.");
            if (decision.Disposition == AdmissionQueueClosingDispositions.Withdraw && string.IsNullOrWhiteSpace(decision.Reason))
                throw new ArgumentException("Withdraw reason is required.");
        }
        var response = _repo.Close(request with { ServicePointId = servicePoint }, date, _clock.Now);
        await _publisher.PublishAsync(null, cancellationToken);
        return response;
    }
}
