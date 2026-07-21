using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IJourneyCandidateFinder
{
    IReadOnlyList<TrkJourneyCandidateDto> Find(string personName, DateOnly tglLahir, DateOnly relevantDate);
}

public class JourneyCandidateFinder : IJourneyCandidateFinder
{
    private readonly IPasienTrackerRepo _trackerRepo;

    public JourneyCandidateFinder(IPasienTrackerRepo trackerRepo)
    {
        _trackerRepo = trackerRepo;
    }

    public IReadOnlyList<TrkJourneyCandidateDto> Find(string personName, DateOnly tglLahir, DateOnly relevantDate)
    {
        var periode = new Periode(relevantDate.ToDateTime(TimeOnly.MinValue));
        var headers = _trackerRepo.ListData(periode, tglLahir)?.ToList() ?? [];
        var personNameEyd = personName.ToEyd();

        var matches = headers
            .Where(x => x.Person.PersonName.ToEyd() == personNameEyd)
            .ToList();

        return matches
            .Select(ToCandidate)
            .ToList();
    }

    private TrkJourneyCandidateDto ToCandidate(PasienTrackerView header)
    {
        var tracker = _trackerRepo.LoadEntity(PasienTrackerModel.Key(header.PasienTrackerId))
            .Match(
                onSome: x => x,
                onNone: () => PasienTrackerModel.Default);

        var events = tracker.PasienTrackerId == header.PasienTrackerId
            ? tracker.ListEvent.Select(e => new TrkJourneyCandidateEventDto(
                e.NoUrut,
                e.EventName,
                e.EventDate.ToString("yyyy-MM-dd HH:mm:ss"),
                e.ReffId))
            : Enumerable.Empty<TrkJourneyCandidateEventDto>();

        return new TrkJourneyCandidateDto(
            header.PasienTrackerId,
            header.Person.PersonName,
            header.Person.TglLahir.ToString("yyyy-MM-dd"),
            header.VisitDate.ToString("yyyy-MM-dd"),
            header.StartPeriod.ToString("yyyy-MM-dd"),
            header.LastPeriod.ToString("yyyy-MM-dd"),
            events);
    }
}
