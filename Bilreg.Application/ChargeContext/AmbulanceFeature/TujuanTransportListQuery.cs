using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public record TujuanTransportListQuery(): IRequest<IEnumerable<TujuanTransportListResponse>>;

public record TujuanTransportListResponse(
    string TujuanTransportId,
    string TujuanTransportName,
    decimal Konstanta,
    bool IsPerkiraan,
    string DefaultAmbulanceId
);

public class TujuanTransportListHandler: IRequestHandler<TujuanTransportListQuery, IEnumerable<TujuanTransportListResponse>>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;

    public TujuanTransportListHandler(ITujuanTransportDal tujuanTransportDal)
    {
        _tujuanTransportDal = tujuanTransportDal;
    }

    public Task<IEnumerable<TujuanTransportListResponse>> Handle(TujuanTransportListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var listTujuanTransport = _tujuanTransportDal.ListData()
            ?? throw new KeyNotFoundException("TujuanTransport not found");
        
        // RESPONSE
        var response = listTujuanTransport.Select(x => new TujuanTransportListResponse(x.TujuanTransportId,
            x.TujuanTransportName, x.Konstanta, x.IsPerkiraan, x.DefaultAmbulanceId));
        return Task.FromResult(response);
    }
}