using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public record BangsalDeleteCommand(string BangsalId) : IRequest, IBangsalKey;

public class BangsalDeleteHandler : IRequestHandler<BangsalDeleteCommand>
{
    private readonly IBangsalWriter _writer;

    public BangsalDeleteHandler(IBangsalWriter writer)
    {
        _writer = writer;
    }
    public Task Handle(BangsalDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.BangsalId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;

    }
}