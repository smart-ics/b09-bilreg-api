using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifDeleteCommand(string TipeTarifId) : IRequest, ITipeTarifKey;

public class TipeTarifDeleteHandler : IRequestHandler<TipeTarifDeleteCommand>
{
    private readonly ITipeTarifWriter _writer;

    public TipeTarifDeleteHandler(ITipeTarifWriter tipeTarifWriter)
    {
        _writer = tipeTarifWriter;
    }
    
    public Task Handle(TipeTarifDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}
