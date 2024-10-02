using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifSaveCommand(string TipeTarifId, string TipeTarifName) : IRequest, ITipeTarifKey;

public class TipeTarifSaveHandler : IRequestHandler<TipeTarifSaveCommand>
{
    private readonly ITipeTarifDal _tipeTarifDal;
    private readonly ITipeTarifWriter _writer;

    public TipeTarifSaveHandler(ITipeTarifDal tipeTarifDal, ITipeTarifWriter writer)
    {
        _tipeTarifDal = tipeTarifDal;
        _writer = writer;
    }
    
    public Task Handle(TipeTarifSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifName);
        
        // BUILD 
        var tipeTarif = _tipeTarifDal.GetData(request)
            ?? new TipeTarifModel(request.TipeTarifId, request.TipeTarifName);
        
        // WRITE
        _writer.Save(tipeTarif);
        return Task.CompletedTask;
    }
}