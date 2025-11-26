using Bilreg.Domain.BillContext.TindakanFeature;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceRemoveKomponenCommand(string AmbulanceId, string KomponenId) : IRequest, IAmbulanceKey, IKomponenKey;
public class AmbulanceRemoveKomponenHandler: IRequestHandler<AmbulanceRemoveKomponenCommand>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceRemoveKomponenHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(AmbulanceRemoveKomponenCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        Guard.IsNotWhiteSpace(request.KomponenId);
        
        // BUILD
        var ambulance = _factory.Load(request);
        ambulance.Remove(x => x.KomponenId == request.KomponenId);
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}