using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record QueSelesaiPeriksaCmd(string AntrianId, int NoUrut)
    : IRequest<QueAntrianEntryActionResponse>, IAntrianKey;

public class QueSelesaiPeriksaHandler : IRequestHandler<QueSelesaiPeriksaCmd, QueAntrianEntryActionResponse>
{
    private readonly IAntrianRepo _queRepo;
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IClinicQueueOperationDal? _clinicQueueDal;
    private readonly IClinicQueueRefreshPublisher _publisher;

    public QueSelesaiPeriksaHandler(
        IAntrianRepo queRepo,
        IPasienTrackerRepo trackerRepo,
        ITglJamProvider tglJamProvider,
        IClinicQueueOperationDal? clinicQueueDal = null,
        IClinicQueueRefreshPublisher? publisher = null)
    {
        _queRepo = queRepo;
        _trackerRepo = trackerRepo;
        _tglJamProvider = tglJamProvider;
        _clinicQueueDal = clinicQueueDal;
        _publisher = publisher ?? new NullClinicQueueRefreshPublisher();
    }

    public async Task<QueAntrianEntryActionResponse> Handle(
        QueSelesaiPeriksaCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        Guard.Against.Null(request.NoUrut);

        var que = _queRepo.LoadEntity(request).GetValueOrDefault();
        var item = que.ListEntry.FirstOrDefault(x => x.NoUrut == request.NoUrut) ??
            throw new KeyNotFoundException($"antrian {request.NoUrut} not found");

        var doneAt = _tglJamProvider.Now;
        item.Done(doneAt);

        var tracker = PhysicianQueueEvidence.RequireTracker(_trackerRepo, item);
        var queueRef = PhysicianQueueEvidence.QueueRef(que, item);
        PhysicianQueueEvidence.AppendConsultDone(tracker, queueRef, doneAt);

        _queRepo.SaveChanges(que);
        _trackerRepo.SaveChanges(tracker);

        _clinicQueueDal?.MarkDismissedByEntry(request.AntrianId, request.NoUrut, "SYSTEM");

        var dokterId = string.Empty;
        var seqTag = que.SequenceTag;
        if (!string.IsNullOrWhiteSpace(seqTag))
        {
            var underscoreIdx = seqTag.LastIndexOf('_');
            if (underscoreIdx >= 0 && underscoreIdx < seqTag.Length - 1)
            {
                dokterId = seqTag[(underscoreIdx + 1)..].Replace('$', ' ');
            }
        }
        var poliId = que.ServicePoint?.ServicePointCode ?? string.Empty;
        await _publisher.PublishRefreshHint(poliId, dokterId, cancellationToken);

        return new QueAntrianEntryActionResponse(
            request.AntrianId,
            request.NoUrut,
            tracker.PasienTrackerId,
            item.AntrianStatus.ToString());
    }
}
