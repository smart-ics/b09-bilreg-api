using Ardalis.GuardClauses;
using Farinv.Domain.SalesContext.AntrianFeature;
using MediatR;

namespace Farinv.Application.SalesContext.AntrianFeature.UsesCases;

public record QueDeliverAntrianCmd(
    string PasienTrackerId,
    int ServicePoint,
    DateTime OccurredAt) : IRequest<PharmacyQueueEntryResponse>;

public class DeliverAntrianHandler : IRequestHandler<QueDeliverAntrianCmd, PharmacyQueueEntryResponse>
{
    public const string ApotekDoneEventName = "Apotek-Done";

    private readonly IAntrianRepo _antrianRepo;
    private readonly IAppendTrackerEvidenceService _appendTrackerEvidenceService;

    public DeliverAntrianHandler(
        IAntrianRepo antrianRepo,
        IAppendTrackerEvidenceService appendTrackerEvidenceService)
    {
        _antrianRepo = antrianRepo;
        _appendTrackerEvidenceService = appendTrackerEvidenceService;
    }

    public Task<PharmacyQueueEntryResponse> Handle(
        QueDeliverAntrianCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienTrackerId);
        Guard.Against.NegativeOrZero(request.ServicePoint);
        PharmacyTrackerIdentity.EnsureRealTrackerId(request.PasienTrackerId);

        var antrian = _antrianRepo.LoadOrCreate(request.ServicePoint);
        var entry = antrian.GetActiveEntryByTracker(request.PasienTrackerId);
        antrian.DeliverSlot(entry.NoAntrian);

        var queueRef = PharmacyQueueEvidenceReference.Create(antrian.AntrianId, entry.NoAntrian);
        _appendTrackerEvidenceService.Execute(new AppendPharmacyEvidenceRequest(
            entry.PasienTrackerId,
            ApotekDoneEventName,
            queueRef,
            request.OccurredAt));

        _antrianRepo.SaveChanges(antrian);

        return Task.FromResult(new PharmacyQueueEntryResponse(
            antrian.AntrianId,
            entry.NoAntrian,
            entry.PasienTrackerId));
    }
}
