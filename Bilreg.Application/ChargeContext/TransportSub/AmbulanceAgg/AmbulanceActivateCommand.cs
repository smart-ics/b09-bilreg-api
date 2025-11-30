using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceActivateCommand(string AmbulanceId): IRequest, IAmbulanceKey;

public class AmbulanceActivateHandler: IRequestHandler<AmbulanceActivateCommand>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceActivateHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(AmbulanceActivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        
        // BUILD
        var ambulance = _factory.Load(request);
        ambulance.SetAktif();
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}