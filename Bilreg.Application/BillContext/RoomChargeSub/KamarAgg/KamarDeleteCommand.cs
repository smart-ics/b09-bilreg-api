using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;

public record KamarDeleteCommand(string KamarId):IRequest,IKamarKey;

public class KamarDeleteHandler : IRequestHandler<KamarDeleteCommand>
{
    private readonly IKamarWriter _writer;

    public KamarDeleteHandler(IKamarWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(KamarDeleteCommand request, CancellationToken cancellationToken)
    {
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.KamarId);
        
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}