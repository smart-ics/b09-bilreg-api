using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record TujuanTransportSaveCommand(
    string TujuanTransportId,
    string TujuanTransportName,
    decimal Konstanta,
    bool IsPerkiraan,
    string DefaultAmbulanceId = "") : IRequest<TujuanTransportSaveResponse>, ITujuanTransportKey;

public record TujuanTransportSaveResponse(string TujuanTransportId);

public class TujuanTransportSaveHandler : IRequestHandler<TujuanTransportSaveCommand, TujuanTransportSaveResponse>
{
    private readonly ITujuanTransportRepo _tujuanTransportRepo;

    public TujuanTransportSaveHandler(ITujuanTransportRepo tujuanTransportRepo)
    {
        _tujuanTransportRepo = tujuanTransportRepo;
    }

    public Task<TujuanTransportSaveResponse> Handle(TujuanTransportSaveCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportId, nameof(request.TujuanTransportId));
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportName, nameof(request.TujuanTransportName));

        var model = _tujuanTransportRepo.LoadEntity(request).Match(
            onSome: existing =>
            {
                existing.SetName(request.TujuanTransportName);
                existing.SetKonstanta(request.Konstanta);
                if (request.IsPerkiraan)
                    existing.SetPerkiraan();
                else
                    existing.UnSetPerkiraan();

                if (!string.IsNullOrWhiteSpace(request.DefaultAmbulanceId))
                    existing.SetDefaultAmbulance(request.DefaultAmbulanceId);
                else
                    existing.UnSetDefaultAmbulance();

                return existing;
            },
            onNone: () =>
            {
                var created = new TujuanTransportModel(
                    request.TujuanTransportId,
                    request.TujuanTransportName,
                    request.Konstanta,
                    request.IsPerkiraan);

                if (!string.IsNullOrWhiteSpace(request.DefaultAmbulanceId))
                    created.SetDefaultAmbulance(request.DefaultAmbulanceId);

                return created;
            });

        _tujuanTransportRepo.SaveChanges(model);
        return Task.FromResult(new TujuanTransportSaveResponse(model.TujuanTransportId));
    }
}
