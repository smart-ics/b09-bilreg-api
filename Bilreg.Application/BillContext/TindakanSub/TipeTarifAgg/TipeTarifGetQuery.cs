using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifGetQuery(string TipeTarifId) : IRequest<TipeTarifGetResponse>, ITipeTarifKey;

public record TipeTarifGetResponse(
    string TipeTarifId,
    string TipeTarifName,
    bool IsAktif,
    decimal NoUrut);
    
public class TipeTarifGetHandler : IRequestHandler<TipeTarifGetQuery, TipeTarifGetResponse>
{
    private readonly ITipeTarifDal _tipeTarifDal;

    public TipeTarifGetHandler(ITipeTarifDal tipeTarifDal)
    {
        _tipeTarifDal = tipeTarifDal;
    }
    
    public Task<TipeTarifGetResponse> Handle(TipeTarifGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var tipeTarif = _tipeTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"No tipe tarif with id {request.TipeTarifId}");
        
        // RESPONSE
        var response = new TipeTarifGetResponse(
            tipeTarif.TipeTarifId,
            tipeTarif.TipeTarifName,
            tipeTarif.IsAktif,
            tipeTarif.NoUrut
            );
        
        return Task.FromResult(response);
    }
}