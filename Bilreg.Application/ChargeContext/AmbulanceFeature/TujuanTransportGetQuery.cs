using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public record TujuanTransportGetQuery(string TujuanTransportId): IRequest<TujuanTransportGetResponse>, ITujuanTransportKey;

public record TujuanTransportGetResponse(
    string TujuanTransportId,
    string TujuanTransportName,
    decimal Konstanta,
    bool IsPerkiraan,
    string DefaultAmbulanceId
);

public class TujuanTransportGetHandler: IRequestHandler<TujuanTransportGetQuery, TujuanTransportGetResponse>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;

    public TujuanTransportGetHandler(ITujuanTransportDal tujuanTransportDal)
    {
        _tujuanTransportDal = tujuanTransportDal;
    }

    public Task<TujuanTransportGetResponse> Handle(TujuanTransportGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var tujuanTransport = _tujuanTransportDal.GetData(request)
            ?? throw new KeyNotFoundException($"TujuanTransport with id {request.TujuanTransportId} not found");
        
        // RESPONSE
        var response = new TujuanTransportGetResponse(tujuanTransport.TujuanTransportId,
            tujuanTransport.TujuanTransportName, tujuanTransport.Konstanta, tujuanTransport.IsPerkiraan,
            tujuanTransport.DefaultAmbulanceId);
        return Task.FromResult(response);
    }
}