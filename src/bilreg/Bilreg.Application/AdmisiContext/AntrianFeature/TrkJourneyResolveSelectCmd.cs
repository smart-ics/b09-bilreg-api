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
    private readonly IAdmissionServicePointResolver _servicePointResolver;

    public TrkJourneyResolveSelectHandler(
        IPasienTrackerRepo trackerRepo,
        IAntrianRepo antrianRepo,
        IAdmissionServicePointResolver servicePointResolver)
    {
        _trackerRepo = trackerRepo;
        _antrianRepo = antrianRepo;
        _servicePointResolver = servicePointResolver;
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

        TrkJourneyResolveSelectResponse response;

        using (var trans = TransHelper.NewScope())
        {
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
            _servicePointResolver.EnsureAdmissionQueue(queue);
            var entry = AdmissionQueueIdentify.RequireAnonymousInServiceEntry(queue, request.NoUrut);

            AdmissionQueueIdentify.IdentifyExistingTrackerAndRecordEvidence(queue, entry, tracker);
            if (!_antrianRepo.TrySaveAnonymousInServiceTransition(queue, entry))
                throw new AdmissionQueueConcurrencyException(
                    $"Queue entry '{queue.AntrianId}' / {entry.NoUrut} was changed concurrently.");
            _trackerRepo.SaveChanges(tracker);
            trans.Complete();

            response = new TrkJourneyResolveSelectResponse(
                tracker.PasienTrackerId,
                queue.AntrianId,
                entry.NoUrut);
        }

        return Task.FromResult(response);
    }
}
