using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record TujuanTransportGetQuery(string TujuanTransportId) : IRequest<TujuanTransportGetResponse>, ITujuanTransportKey;

public record TujuanTransportGetResponse(
    string TujuanTransportId,
    string TujuanTransportName,
    decimal Konstanta,
    bool IsPerkiraan,
    string DefaultAmbulanceId);

public class TujuanTransportGetHandler : IRequestHandler<TujuanTransportGetQuery, TujuanTransportGetResponse>
{
    private readonly ITujuanTransportRepo _tujuanTransportRepo;

    public TujuanTransportGetHandler(ITujuanTransportRepo tujuanTransportRepo)
    {
        _tujuanTransportRepo = tujuanTransportRepo;
    }

    public Task<TujuanTransportGetResponse> Handle(TujuanTransportGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportId, nameof(request.TujuanTransportId));

        var model = _tujuanTransportRepo.LoadEntity(request).Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException($"Tujuan Transport dengan id '{request.TujuanTransportId}' tidak ditemukan."));

        var response = new TujuanTransportGetResponse(
            model.TujuanTransportId,
            model.TujuanTransportName,
            model.Konstanta,
            model.IsPerkiraan,
            model.DefaultAmbulanceId);

        return Task.FromResult(response);
    }
}
