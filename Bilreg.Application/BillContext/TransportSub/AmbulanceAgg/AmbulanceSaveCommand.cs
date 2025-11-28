using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceSaveCommand(string AmbulanceId, string AmbulanceName, decimal Abonement): IRequest, IAmbulanceKey;

public class AmbulanceSaveHandler: IRequestHandler<AmbulanceSaveCommand>
{
    private readonly IFactoryLoadOrNull<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceSaveHandler(IFactoryLoadOrNull<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(AmbulanceSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        Guard.IsNotWhiteSpace(request.AmbulanceName);
        
        // BUILD
        var ambulance = _factory.LoadOrNull(request)
            ?? new AmbulanceModel(request.AmbulanceId, request.AmbulanceName);
        ambulance.SetName(request.AmbulanceName);
        ambulance.SetAbonement(request.Abonement);
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}