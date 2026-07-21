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
    string AntrianId,
    int NoUrut,
    string UserId) : IRequest<TrkJourneyResolveNewResponse>;

public record TrkJourneyResolveNewResponse(
    string PasienTrackerId,
    string AntrianId,
    int NoUrut);

public class TrkJourneyResolveNewHandler
    : IRequestHandler<TrkJourneyResolveNewCmd, TrkJourneyResolveNewResponse>
{
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TrkJourneyResolveNewHandler(
        IPasienTrackerRepo trackerRepo,
        IAntrianRepo antrianRepo,
        ITglJamProvider tglJamProvider)
    {
        _trackerRepo = trackerRepo;
        _antrianRepo = antrianRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TrkJourneyResolveNewResponse> Handle(
        TrkJourneyResolveNewCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PersonName);
        Guard.Against.NullOrWhiteSpace(request.TglLahir);
        Guard.Against.NullOrWhiteSpace(request.VisitDate);
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.NoUrut <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.NoUrut));

        var queue = _antrianRepo.LoadEntity(AntrianModel.Key(request.AntrianId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Antrian '{request.AntrianId}' not found"));

        var entry = AdmissionQueueIdentify.RequireAnonymousWaitingEntry(queue, request.NoUrut);
        var queueRef = QueueEvidenceReference.Create(queue.AntrianId, entry.NoUrut).Value;
        var tglLahir = DateOnly.ParseExact(request.TglLahir, "yyyy-MM-dd");
        var visitDate = DateOnly.ParseExact(request.VisitDate, "yyyy-MM-dd");
        var person = new PersonType(request.PersonName, tglLahir);
        var tracker = PasienTrackerModel.Create(
            person,
            visitDate,
            AdmissionQueueIdentify.CheckInEventName,
            queueRef,
            entry.CreatedAt);

        var servedAt = _tglJamProvider.Now;

        using (var trans = TransHelper.NewScope())
        {
            AdmissionQueueIdentify.IdentifyAndRecordEvidence(queue, entry, tracker, servedAt);
            _antrianRepo.SaveChanges(queue);
            _trackerRepo.SaveChanges(tracker);
            trans.Complete();
        }

        return Task.FromResult(new TrkJourneyResolveNewResponse(
            tracker.PasienTrackerId,
            queue.AntrianId,
            entry.NoUrut));
    }
}
