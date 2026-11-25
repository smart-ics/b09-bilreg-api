using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record ListActiveDispatchQuery() : IRequest<IEnumerable<TransportGetResponse>>;

public class ListActiveDispatchHandler : IRequestHandler<ListActiveDispatchQuery, IEnumerable<TransportGetResponse>>
{
    private readonly ITrsTransportRepo _trsTransportRepo;

    public ListActiveDispatchHandler(ITrsTransportRepo trsTransportRepo)
    {
        _trsTransportRepo = trsTransportRepo;
    }

    public Task<IEnumerable<TransportGetResponse>> Handle(ListActiveDispatchQuery request, CancellationToken cancellationToken)
    {
        var list = _trsTransportRepo.ListActiveDispatched() ?? [];
        var response = list.Select(t => TransportResponseMapper.ToResponse(t)).ToList();

        return Task.FromResult<IEnumerable<TransportGetResponse>>(response);
    }
}
