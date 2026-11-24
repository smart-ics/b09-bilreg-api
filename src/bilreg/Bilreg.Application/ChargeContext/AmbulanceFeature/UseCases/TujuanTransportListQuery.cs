using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record TujuanTransportListQuery() : IRequest<IEnumerable<TujuanTransportGetResponse>>;

public record ListTujuanTransportQuery() : IRequest<IEnumerable<TujuanTransportGetResponse>>;

public class TujuanTransportListHandler :
    IRequestHandler<TujuanTransportListQuery, IEnumerable<TujuanTransportGetResponse>>,
    IRequestHandler<ListTujuanTransportQuery, IEnumerable<TujuanTransportGetResponse>>
{
    private readonly ITujuanTransportRepo _tujuanTransportRepo;

    public TujuanTransportListHandler(ITujuanTransportRepo tujuanTransportRepo)
    {
        _tujuanTransportRepo = tujuanTransportRepo;
    }

    public Task<IEnumerable<TujuanTransportGetResponse>> Handle(TujuanTransportListQuery request, CancellationToken cancellationToken)
    {
        return HandleInternal();
    }

    public Task<IEnumerable<TujuanTransportGetResponse>> Handle(ListTujuanTransportQuery request, CancellationToken cancellationToken)
    {
        return HandleInternal();
    }

    private Task<IEnumerable<TujuanTransportGetResponse>> HandleInternal()
    {
        var list = _tujuanTransportRepo.ListData() ?? [];
        var result = list.Select(m => new TujuanTransportGetResponse(
            m.TujuanTransportId,
            m.TujuanTransportName,
            m.Konstanta,
            m.IsPerkiraan,
            m.DefaultAmbulanceId)).ToList();

        return Task.FromResult<IEnumerable<TujuanTransportGetResponse>>(result);
    }
}
