using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record TrkJourneyResolveSelectCmd(
    string PasienTrackerId,
    string AntrianId,
    int NoUrut,
    string UserId) : IRequest<TrkJourneyResolveSelectResponse>;

public record TrkJourneyResolveSelectResponse(
    string PasienTrackerId,
    string AntrianId,
    int NoUrut);

public class TrkJourneyResolveSelectHandler
    : IRequestHandler<TrkJourneyResolveSelectCmd, TrkJourneyResolveSelectResponse>
{
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TrkJourneyResolveSelectHandler(
        IPasienTrackerRepo trackerRepo,
        IAntrianRepo antrianRepo,
        ITglJamProvider tglJamProvider)
    {
        _trackerRepo = trackerRepo;
        _antrianRepo = antrianRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TrkJourneyResolveSelectResponse> Handle(
        TrkJourneyResolveSelectCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienTrackerId);
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.NoUrut <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.NoUrut));

        var tracker = _trackerRepo.LoadEntity(PasienTrackerModel.Key(request.PasienTrackerId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"PasienTracker '{request.PasienTrackerId}' not found"));

        var queue = _antrianRepo.LoadEntity(AntrianModel.Key(request.AntrianId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Antrian '{request.AntrianId}' not found"));

        var entry = AdmissionQueueIdentify.RequireAnonymousWaitingEntry(queue, request.NoUrut);
        var servedAt = _tglJamProvider.Now;

        using (var trans = TransHelper.NewScope())
        {
            AdmissionQueueIdentify.IdentifyAndRecordEvidence(queue, entry, tracker, servedAt);
            _antrianRepo.SaveChanges(queue);
            _trackerRepo.SaveChanges(tracker);
            trans.Complete();
        }

        return Task.FromResult(new TrkJourneyResolveSelectResponse(
            tracker.PasienTrackerId,
            queue.AntrianId,
            entry.NoUrut));
    }
}
