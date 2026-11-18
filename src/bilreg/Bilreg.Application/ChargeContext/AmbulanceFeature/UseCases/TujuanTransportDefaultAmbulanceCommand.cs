using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record TujuanTransportSetDefaultAmbulanceCommand(
    string TujuanTransportId,
    string DefaultAmbulanceId) : IRequest, ITujuanTransportKey;

public record TujuanTransportUnSetDefaultAmbulanceCommand(
    string TujuanTransportId) : IRequest, ITujuanTransportKey;

public class TujuanTransportSetDefaultAmbulanceHandler : IRequestHandler<TujuanTransportSetDefaultAmbulanceCommand>
{
    private readonly ITujuanTransportRepo _tujuanTransportRepo;

    public TujuanTransportSetDefaultAmbulanceHandler(ITujuanTransportRepo tujuanTransportRepo)
    {
        _tujuanTransportRepo = tujuanTransportRepo;
    }

    public Task Handle(TujuanTransportSetDefaultAmbulanceCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportId, nameof(request.TujuanTransportId));
        Guard.Against.NullOrWhiteSpace(request.DefaultAmbulanceId, nameof(request.DefaultAmbulanceId));

        var model = _tujuanTransportRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Tujuan Transport dengan id '{request.TujuanTransportId}' tidak ditemukan."));

        model.SetDefaultAmbulance(request.DefaultAmbulanceId);
        _tujuanTransportRepo.SaveChanges(model);
        return Task.CompletedTask;
    }
}

public class TujuanTransportUnSetDefaultAmbulanceHandler : IRequestHandler<TujuanTransportUnSetDefaultAmbulanceCommand>
{
    private readonly ITujuanTransportRepo _tujuanTransportRepo;

    public TujuanTransportUnSetDefaultAmbulanceHandler(ITujuanTransportRepo tujuanTransportRepo)
    {
        _tujuanTransportRepo = tujuanTransportRepo;
    }

    public Task Handle(TujuanTransportUnSetDefaultAmbulanceCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportId, nameof(request.TujuanTransportId));

        var model = _tujuanTransportRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Tujuan Transport dengan id '{request.TujuanTransportId}' tidak ditemukan."));

        model.UnSetDefaultAmbulance();
        _tujuanTransportRepo.SaveChanges(model);
        return Task.CompletedTask;
    }
}
