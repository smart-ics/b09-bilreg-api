using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceDeactivateCommand(string AmbulanceId): IRequest, IAmbulanceKey;

public class AmbulanceDeactivateHandler: IRequestHandler<AmbulanceDeactivateCommand>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceDeactivateHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(AmbulanceDeactivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        
        // BUILD
        var ambulance = _factory.Load(request);
        ambulance.UnSetAktif();
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}