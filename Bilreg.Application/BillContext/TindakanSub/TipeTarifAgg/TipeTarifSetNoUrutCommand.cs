using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifSetNoUrutCommand(string TipeTarifId, decimal NoUrut) : IRequest, ITipeTarifKey;

public class TipeTarifSetNoUrutHandler : IRequestHandler<TipeTarifSetNoUrutCommand>
{
    private readonly ITipeTarifDal _tipeTarifDal;
    private readonly ITipeTarifWriter _writer;

    public TipeTarifSetNoUrutHandler(ITipeTarifDal tipeTarifDal, ITipeTarifWriter writer)
    {
        _tipeTarifDal = tipeTarifDal;
        _writer = writer;
    }
    
    public Task Handle(TipeTarifSetNoUrutCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        
        // BUILD
        var tipeTarifList = _tipeTarifDal.ListData()
            ?? throw new KeyNotFoundException("No tipe tarif");
        
        var List = tipeTarifList.ToList().OrderBy(x => x.NoUrut).ToList();
        var result = ReOrder(List, request.TipeTarifId, request.NoUrut);
        
        // WRITE
        List.ForEach(x => _writer.Save(x));
        return Task.CompletedTask;
    }

    private IEnumerable<TipeTarifModel> ReOrder(List<TipeTarifModel> list, string id, decimal target)
    {
        var targetItem = list.FirstOrDefault(x => x.NoUrut == target);
        return targetItem is null ?
            replaceNoUrut(list, id, target) :
            reOrderNoUrut(list, id, target);
            
    }

    private IEnumerable<TipeTarifModel> reOrderNoUrut(List<TipeTarifModel> list, string id, decimal target)
    {
        throw new NotImplementedException();
    }

    private IEnumerable<TipeTarifModel> replaceNoUrut(List<TipeTarifModel> list, string id, decimal target)
    {
        throw new NotImplementedException();
    }
}
