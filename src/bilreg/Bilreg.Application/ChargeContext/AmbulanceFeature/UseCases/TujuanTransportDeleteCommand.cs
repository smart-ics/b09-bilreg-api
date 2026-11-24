using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record TujuanTransportDeleteCommand(string TujuanTransportId) : IRequest, ITujuanTransportKey;

public class TujuanTransportDeleteHandler : IRequestHandler<TujuanTransportDeleteCommand>
{
    private readonly ITujuanTransportRepo _tujuanTransportRepo;

    public TujuanTransportDeleteHandler(ITujuanTransportRepo tujuanTransportRepo)
    {
        _tujuanTransportRepo = tujuanTransportRepo;
    }

    public Task Handle(TujuanTransportDeleteCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TujuanTransportId, nameof(request.TujuanTransportId));

        _tujuanTransportRepo.Delete(request);
        return Task.CompletedTask;
    }
}
