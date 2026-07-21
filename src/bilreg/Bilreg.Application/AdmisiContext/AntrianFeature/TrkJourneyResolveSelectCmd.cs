using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record TrkJourneyResolveSelectCmd(
    string PasienTrackerId,
    string UserId) : IRequest<TrkJourneyResolveSelectResponse>;

public record TrkJourneyResolveSelectResponse(string PasienTrackerId);

public class TrkJourneyResolveSelectHandler
    : IRequestHandler<TrkJourneyResolveSelectCmd, TrkJourneyResolveSelectResponse>
{
    private readonly IPasienTrackerRepo _trackerRepo;

    public TrkJourneyResolveSelectHandler(IPasienTrackerRepo trackerRepo)
    {
        _trackerRepo = trackerRepo;
    }

    public Task<TrkJourneyResolveSelectResponse> Handle(
        TrkJourneyResolveSelectCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienTrackerId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var tracker = _trackerRepo.LoadEntity(PasienTrackerModel.Key(request.PasienTrackerId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"PasienTracker '{request.PasienTrackerId}' not found"));

        return Task.FromResult(new TrkJourneyResolveSelectResponse(tracker.PasienTrackerId));
    }
}
