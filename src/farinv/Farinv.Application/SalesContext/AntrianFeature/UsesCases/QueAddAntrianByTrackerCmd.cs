using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Farinv.Domain.SalesContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Farinv.Application.SalesContext.AntrianFeature.UsesCases;

public record QueAddAntrianByTrackerCmd(
    string RegId,
    string PasienTrackerId,
    int ServicePoint,
    int NoAntrian,
    DateTime? CreatedAt) : IRequest<PharmacyQueueEntryResponse>, IRegKey;

public class AddAntrianByTrackerHandler : IRequestHandler<QueAddAntrianByTrackerCmd, PharmacyQueueEntryResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IRegRepo _regRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AddAntrianByTrackerHandler(
        IAntrianRepo antrianRepo,
        IRegRepo regRepo,
        ITglJamProvider tglJamProvider)
    {
        _antrianRepo = antrianRepo;
        _regRepo = regRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<PharmacyQueueEntryResponse> Handle(
        QueAddAntrianByTrackerCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.PasienTrackerId);
        Guard.Against.NegativeOrZero(request.NoAntrian);
        Guard.Against.NegativeOrZero(request.ServicePoint);
        PharmacyTrackerIdentity.EnsureRealTrackerId(request.PasienTrackerId);

        var reg = LoadReg(request);
        var antrian = _antrianRepo.LoadOrCreate(request.ServicePoint);
        var takenAt = request.CreatedAt ?? _tglJamProvider.Now;
        var entry = antrian.AddEntryByTracker(request.NoAntrian, reg, request.PasienTrackerId, takenAt);

        _antrianRepo.SaveChanges(antrian);

        var response = new PharmacyQueueEntryResponse(
            antrian.AntrianId,
            entry.NoAntrian,
            entry.PasienTrackerId);

        return Task.FromResult(response);
    }

    private RegReff LoadReg(QueAddAntrianByTrackerCmd request)
    {
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Register {request.RegId} not found");
        return reg.ToReff();
    }
}
