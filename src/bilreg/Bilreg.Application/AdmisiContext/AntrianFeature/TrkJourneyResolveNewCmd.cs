using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record TrkJourneyResolveNewCmd(
    string PersonName,
    string TglLahir,
    string VisitDate,
    string UserId,
    string EventName,
    string ReffId) : IRequest<TrkJourneyResolveNewResponse>;

public record TrkJourneyResolveNewResponse(string PasienTrackerId);

public class TrkJourneyResolveNewHandler
    : IRequestHandler<TrkJourneyResolveNewCmd, TrkJourneyResolveNewResponse>
{
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TrkJourneyResolveNewHandler(
        IPasienTrackerRepo trackerRepo,
        ITglJamProvider tglJamProvider)
    {
        _trackerRepo = trackerRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TrkJourneyResolveNewResponse> Handle(
        TrkJourneyResolveNewCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PersonName);
        Guard.Against.NullOrWhiteSpace(request.TglLahir);
        Guard.Against.NullOrWhiteSpace(request.VisitDate);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.EventName);
        Guard.Against.NullOrWhiteSpace(request.ReffId);

        var occurredAt = _tglJamProvider.Now;
        var tglLahir = DateOnly.ParseExact(request.TglLahir, "yyyy-MM-dd");
        var visitDate = DateOnly.ParseExact(request.VisitDate, "yyyy-MM-dd");
        var person = new PersonType(request.PersonName, tglLahir);
        var tracker = PasienTrackerModel.Create(
            person, visitDate, request.EventName, request.ReffId, occurredAt);

        using (var trans = TransHelper.NewScope())
        {
            _trackerRepo.SaveChanges(tracker);
            trans.Complete();
        }

        return Task.FromResult(new TrkJourneyResolveNewResponse(tracker.PasienTrackerId));
    }
}
