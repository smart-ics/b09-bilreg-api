using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record ListTransportByRegQuery(string RegId) : IRequest<IEnumerable<TransportGetResponse>>, IRegKey;

public class ListTransportByRegHandler : IRequestHandler<ListTransportByRegQuery, IEnumerable<TransportGetResponse>>
{
    private readonly ITrsTransportRepo _trsTransportRepo;

    public ListTransportByRegHandler(ITrsTransportRepo trsTransportRepo)
    {
        _trsTransportRepo = trsTransportRepo;
    }

    public Task<IEnumerable<TransportGetResponse>> Handle(ListTransportByRegQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var list = _trsTransportRepo.ListDataByReg(request.RegId) ?? [];
        var response = list.Select(TransportResponseMapper.ToResponse).ToList();

        return Task.FromResult<IEnumerable<TransportGetResponse>>(response);
    }
}
