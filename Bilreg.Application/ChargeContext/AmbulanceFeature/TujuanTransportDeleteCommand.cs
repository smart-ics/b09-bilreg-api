using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature;

public record TujuanTransportDeleteCommand(string TujuanTransportId): IRequest, ITujuanTransportKey;

public class TujuanTransportDeleteHandler: IRequestHandler<TujuanTransportDeleteCommand>
{
    private readonly ITujuanTransportWriter _writer;

    public TujuanTransportDeleteHandler(ITujuanTransportWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(TujuanTransportDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.TujuanTransportId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}