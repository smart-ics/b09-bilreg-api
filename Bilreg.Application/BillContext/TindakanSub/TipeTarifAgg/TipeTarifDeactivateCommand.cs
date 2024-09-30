using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifDeactivateCommand(string TipeTarifId) : IRequest, ITipeTarifKey;

public class TipeTarifDeactivateHandler : IRequestHandler<TipeTarifDeactivateCommand>
{
    private readonly ITipeTarifDal _tipeTarifDal;
    private readonly ITipeTarifWriter _writer;

    public TipeTarifDeactivateHandler(ITipeTarifDal tipeTarifDal, ITipeTarifWriter writer)
    {
        _tipeTarifDal = tipeTarifDal;
        _writer = writer;
    }
    
    public Task Handle(TipeTarifDeactivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        
        // BUILD 
        var statusDeactive = _tipeTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tipe Tarif {request.TipeTarifId} not found");
        
        statusDeactive.Deactivate();
        _writer.Save(statusDeactive);
        return Task.CompletedTask;

    }
}
