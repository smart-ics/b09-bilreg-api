using Ardalis.GuardClauses;
using Farinv.Domain.SalesContext.AntrianFeature;
using MediatR;

namespace Farinv.Application.SalesContext.AntrianFeature.UsesCases;

public record QueConfirmPharmacySaleCmd(
    string PasienTrackerId,
    int ServicePoint,
    string PenjualanId,
    DateTime OccurredAt) : IRequest<PharmacyQueueEntryResponse>;

public class ConfirmPharmacySaleHandler : IRequestHandler<QueConfirmPharmacySaleCmd, PharmacyQueueEntryResponse>
{
    public const string ApotekStartEventName = "Apotek-Start";

    private readonly IAntrianRepo _antrianRepo;
    private readonly IAppendTrackerEvidenceService _appendTrackerEvidenceService;

    public ConfirmPharmacySaleHandler(
        IAntrianRepo antrianRepo,
        IAppendTrackerEvidenceService appendTrackerEvidenceService)
    {
        _antrianRepo = antrianRepo;
        _appendTrackerEvidenceService = appendTrackerEvidenceService;
    }

    public Task<PharmacyQueueEntryResponse> Handle(
        QueConfirmPharmacySaleCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienTrackerId);
        Guard.Against.NullOrWhiteSpace(request.PenjualanId);
        Guard.Against.NegativeOrZero(request.ServicePoint);
        PharmacyTrackerIdentity.EnsureRealTrackerId(request.PasienTrackerId);

        var antrian = _antrianRepo.LoadOrCreate(request.ServicePoint);
        var entry = antrian.GetActiveEntryByTracker(request.PasienTrackerId);
        antrian.ConfirmPharmacySale(entry.NoAntrian, request.PenjualanId, request.OccurredAt);

        _appendTrackerEvidenceService.Execute(new AppendPharmacyEvidenceRequest(
            entry.PasienTrackerId,
            ApotekStartEventName,
            request.PenjualanId,
            request.OccurredAt));

        _antrianRepo.SaveChanges(antrian);

        return Task.FromResult(new PharmacyQueueEntryResponse(
            antrian.AntrianId,
            entry.NoAntrian,
            entry.PasienTrackerId));
    }
}
