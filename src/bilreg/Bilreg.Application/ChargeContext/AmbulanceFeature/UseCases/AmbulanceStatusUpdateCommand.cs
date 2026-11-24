using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record AmbulanceActivateCommand(string AmbulanceId) : IRequest, IAmbulanceKey;

public record AmbulanceDeactivateCommand(string AmbulanceId) : IRequest, IAmbulanceKey;

public class AmbulanceActivateHandler : IRequestHandler<AmbulanceActivateCommand>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceActivateHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task Handle(AmbulanceActivateCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));

        var ambulance = _ambulanceRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Ambulans dengan id '{request.AmbulanceId}' tidak ditemukan."));

        ambulance.SetAktif();
        _ambulanceRepo.SaveChanges(ambulance);
        return Task.CompletedTask;
    }
}

public class AmbulanceDeactivateHandler : IRequestHandler<AmbulanceDeactivateCommand>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceDeactivateHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task Handle(AmbulanceDeactivateCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));

        var ambulance = _ambulanceRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Ambulans dengan id '{request.AmbulanceId}' tidak ditemukan."));

        ambulance.UnSetAktif();
        _ambulanceRepo.SaveChanges(ambulance);
        return Task.CompletedTask;
    }
}
