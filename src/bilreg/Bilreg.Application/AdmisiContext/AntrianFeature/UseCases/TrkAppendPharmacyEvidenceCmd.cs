using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record TrkAppendPharmacyEvidenceCmd(
    string PasienTrackerId,
    string EventName,
    string ReffId,
    DateTime OccurredAt) : IRequest, IPasienTrackerKey;

public class TrkAppendPharmacyEvidenceHandler : IRequestHandler<TrkAppendPharmacyEvidenceCmd>
{
    private readonly IPasienTrackerRepo _trackerRepo;

    public TrkAppendPharmacyEvidenceHandler(IPasienTrackerRepo trackerRepo)
    {
        _trackerRepo = trackerRepo;
    }

    public Task Handle(TrkAppendPharmacyEvidenceCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienTrackerId);
        Guard.Against.NullOrWhiteSpace(request.EventName);
        Guard.Against.NullOrWhiteSpace(request.ReffId);

        var tracker = PharmacyQueueEvidence.RequireTracker(_trackerRepo, request.PasienTrackerId);
        PharmacyQueueEvidence.Append(tracker, request.EventName, request.ReffId, request.OccurredAt);
        _trackerRepo.SaveChanges(tracker);

        return Task.CompletedTask;
    }
}
