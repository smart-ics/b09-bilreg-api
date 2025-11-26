using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public record TujuanTransportUnSetDefaultAmbulanceCommand(string TujuanTransportId): IRequest, ITujuanTransportKey;

public class TujuanTransportUnSetDefaultAmbulanceHandler: IRequestHandler<TujuanTransportUnSetDefaultAmbulanceCommand>
{
    private readonly ITujuanTransportDal _tujuanTransportDal;
    private readonly ITujuanTransportWriter _writer;

    public TujuanTransportUnSetDefaultAmbulanceHandler(ITujuanTransportDal tujuanTransportDal, ITujuanTransportWriter writer)
    {
        _tujuanTransportDal = tujuanTransportDal;
        _writer = writer;
    }

    public Task Handle(TujuanTransportUnSetDefaultAmbulanceCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        var tujuanTransport = _tujuanTransportDal.GetData(request)
            ?? throw new KeyNotFoundException($"TujuanTransport with id {request.TujuanTransportId} not found");
        
        // BUILD
        tujuanTransport.UnSetDefaultAmbulance();
        
        // WRITE
        _ = _writer.Save(tujuanTransport);
        return Task.CompletedTask;
    }
}