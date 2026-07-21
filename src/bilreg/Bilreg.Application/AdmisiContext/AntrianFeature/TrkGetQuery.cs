using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record TrkGetQuery(string PasienTrackerId) : IRequest<TrkGetResponse>, IPasienTrackerKey;

public record TrkGetResponse(
    string PasienTrackerId,
    string PersonName,
    string TglLahir,
    string VisitDate,
    string StartPeriod,
    string LastPeriod,
    IEnumerable<TrkJourneyCandidateEventDto> ListEvent);

public class TrkGetHandler : IRequestHandler<TrkGetQuery, TrkGetResponse>
{
    private readonly IPasienTrackerRepo _trackerRepo;

    public TrkGetHandler(IPasienTrackerRepo trackerRepo)
    {
        _trackerRepo = trackerRepo;
    }

    public Task<TrkGetResponse> Handle(TrkGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienTrackerId);

        var tracker = _trackerRepo.LoadEntity(PasienTrackerModel.Key(request.PasienTrackerId))
            .GetValueOrThrow($"PasienTracker '{request.PasienTrackerId}' not found");

        var listEvent = tracker.ListEvent
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.NoUrut)
            .Select(e => new TrkJourneyCandidateEventDto(
                e.NoUrut,
                e.EventName,
                e.EventDate.ToString("yyyy-MM-dd HH:mm:ss"),
                e.ReffId))
            .ToList();

        var response = new TrkGetResponse(
            tracker.PasienTrackerId,
            tracker.Person.PersonName,
            tracker.Person.TglLahir.ToString("yyyy-MM-dd"),
            tracker.VisitDate.ToString("yyyy-MM-dd"),
            tracker.StartPeriod.ToString("yyyy-MM-dd"),
            tracker.LastPeriod.ToString("yyyy-MM-dd"),
            listEvent);

        return Task.FromResult(response);
    }
}
