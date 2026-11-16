using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record AmbulanceAddTujuanCommand(
    string AmbulanceId,
    string TujuanTransportId,
    decimal Konstanta = 0) : IRequest, IAmbulanceKey, ITujuanTransportKey;

public record AmbulanceRemoveTujuanCommand(
    string AmbulanceId,
    string TujuanTransportId) : IRequest, IAmbulanceKey, ITujuanTransportKey;

public class AmbulanceAddTujuanHandler : IRequestHandler<AmbulanceAddTujuanCommand>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceAddTujuanHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task Handle(AmbulanceAddTujuanCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportId, nameof(request.TujuanTransportId));

        var ambulance = _ambulanceRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Ambulans dengan id '{request.AmbulanceId}' tidak ditemukan."));

        ambulance.RemoveTujuan(x => x.TujuanTransportId == request.TujuanTransportId);
        ambulance.AddTujuan(new TransportTujuanModel(ambulance.AmbulanceId, request.TujuanTransportId, request.Konstanta));

        _ambulanceRepo.SaveChanges(ambulance);
        return Task.CompletedTask;
    }
}

public class AmbulanceRemoveTujuanHandler : IRequestHandler<AmbulanceRemoveTujuanCommand>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceRemoveTujuanHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task Handle(AmbulanceRemoveTujuanCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportId, nameof(request.TujuanTransportId));

        var ambulance = _ambulanceRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Ambulans dengan id '{request.AmbulanceId}' tidak ditemukan."));

        ambulance.RemoveTujuan(x => x.TujuanTransportId == request.TujuanTransportId);

        _ambulanceRepo.SaveChanges(ambulance);
        return Task.CompletedTask;
    }
}
