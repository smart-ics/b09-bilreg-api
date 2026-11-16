using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record AmbulanceAddKomponenCommand(
    string AmbulanceId,
    string DetilTarifId,
    decimal Tarif,
    bool IsTetap = false) : IRequest, IAmbulanceKey;

public record AmbulanceRemoveKomponenCommand(
    string AmbulanceId,
    string DetilTarifId) : IRequest, IAmbulanceKey;

public class AmbulanceAddKomponenHandler : IRequestHandler<AmbulanceAddKomponenCommand>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceAddKomponenHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task Handle(AmbulanceAddKomponenCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));
        Guard.Against.NullOrWhiteSpace(request.DetilTarifId, nameof(request.DetilTarifId));

        var ambulance = _ambulanceRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Ambulans dengan id '{request.AmbulanceId}' tidak ditemukan."));

        ambulance.Remove(x => x.KomponenId == request.DetilTarifId);
        ambulance.Add(new AmbulanceKomponenModel(ambulance.AmbulanceId, request.DetilTarifId, request.Tarif, request.IsTetap));

        _ambulanceRepo.SaveChanges(ambulance);
        return Task.CompletedTask;
    }
}

public class AmbulanceRemoveKomponenHandler : IRequestHandler<AmbulanceRemoveKomponenCommand>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceRemoveKomponenHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task Handle(AmbulanceRemoveKomponenCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));
        Guard.Against.NullOrWhiteSpace(request.DetilTarifId, nameof(request.DetilTarifId));

        var ambulance = _ambulanceRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Ambulans dengan id '{request.AmbulanceId}' tidak ditemukan."));

        ambulance.Remove(x => x.KomponenId == request.DetilTarifId);

        _ambulanceRepo.SaveChanges(ambulance);
        return Task.CompletedTask;
    }
}
