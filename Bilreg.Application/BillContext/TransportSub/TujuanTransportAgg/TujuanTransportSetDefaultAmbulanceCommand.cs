using Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public record TujuanTransportSetDefaultAmbulanceCommand(string TujuanTransportId, string AmbulanceId): IRequest, ITujuanTransportKey, IAmbulanceKey;

public class TujuanTransportSetDefaultAmbulanceHandler: IRequestHandler<TujuanTransportSetDefaultAmbulanceCommand>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;
    private readonly IAmbulanceDal _ambulanceDal;
    private readonly ITujuanTransportWriter _writer;
    
    public TujuanTransportSetDefaultAmbulanceHandler(ITujuanTransportDal tujuanTransportDal, IAmbulanceDal ambulanceDal, ITujuanTransportWriter writer)
    {
        _tujuanTransportDal = tujuanTransportDal;
        _ambulanceDal = ambulanceDal;
        _writer = writer;
    }

    public Task Handle(TujuanTransportSetDefaultAmbulanceCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.TujuanTransportId);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        var tujuanTransport = _tujuanTransportDal.GetData(request)
            ?? throw new KeyNotFoundException($"TujuanTransport with id {request.TujuanTransportId} not found");
        var ambulance = _ambulanceDal.GetData(request)
            ?? throw new KeyNotFoundException($"Ambulance with id {request.AmbulanceId} not found");
        
        // BUILD
        tujuanTransport.SetDefaultAmbulance(ambulance);
        
        // WRITE
        _ = _writer.Save(tujuanTransport);
        return Task.CompletedTask;
    }
}