using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifListQuery() : IRequest<IEnumerable<TipeTarifListResponse>>, IRequest;

public record TipeTarifListResponse(
    string TipeTarifId,
    string TipeTarifName,
    bool IsAktif,
    decimal NoUrut);

public class TipeTarifListHandler : IRequestHandler<TipeTarifListQuery>
{
    private readonly ITipeTarifDal _tipeTarifDal;

    public TipeTarifListHandler(ITipeTarifDal tipeTarifDal)
    {
        _tipeTarifDal = tipeTarifDal;
    }
    
    public Task Handle(TipeTarifListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var tipetarifList  = _tipeTarifDal.ListData()
            ?? throw new KeyNotFoundException("Tipe Tarif not found");
        
        // RESPONSE
        var response = tipetarifList.Select(x => new TipeTarifListResponse(
            x.TipeTarifId,
            x.TipeTarifName,
            x.IsAktif,
            x.NoUrut));
        
        return Task.FromResult(response);
    }
}