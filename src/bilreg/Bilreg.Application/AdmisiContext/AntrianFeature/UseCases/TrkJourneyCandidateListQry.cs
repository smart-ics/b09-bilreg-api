using Ardalis.GuardClauses;
using MediatR;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record TrkJourneyCandidateListQry(
    string PersonName,
    string TglLahir,
    string RelevantDate) : IRequest<IEnumerable<TrkJourneyCandidateDto>>;

public class TrkJourneyCandidateListHandler
    : IRequestHandler<TrkJourneyCandidateListQry, IEnumerable<TrkJourneyCandidateDto>>
{
    private readonly IJourneyCandidateFinder _candidateFinder;

    public TrkJourneyCandidateListHandler(IJourneyCandidateFinder candidateFinder)
    {
        _candidateFinder = candidateFinder;
    }

    public Task<IEnumerable<TrkJourneyCandidateDto>> Handle(
        TrkJourneyCandidateListQry request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PersonName);
        Guard.Against.NullOrWhiteSpace(request.TglLahir);
        Guard.Against.NullOrWhiteSpace(request.RelevantDate);

        var tglLahir = DateOnly.ParseExact(request.TglLahir, "yyyy-MM-dd");
        var relevantDate = DateOnly.ParseExact(request.RelevantDate, "yyyy-MM-dd");
        var candidates = _candidateFinder.Find(request.PersonName, tglLahir, relevantDate);
        return Task.FromResult<IEnumerable<TrkJourneyCandidateDto>>(candidates);
    }
}
