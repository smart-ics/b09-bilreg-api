using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfListTarifBrgQuery(string Keyword, string LayananId, string KelasId, string TipeTarifId) 
    : IRequest<IEnumerable<TrfListTarifBrgResponseBase>>, ILayananKey, INilaiTarifVariant;

public abstract record TrfListTarifBrgResponseBase(string Flag);

public record TrfListTarif(string TarifId, string TarifName, decimal Harga) : TrfListTarifBrgResponseBase("TARIF");
public record TrfListBrg(string BrgId, string BrgName, int Qty, string Satuan) : TrfListTarifBrgResponseBase("BRG");

public class TrfListTarifBrgHandler : IRequestHandler<TrfListTarifBrgQuery, IEnumerable<TrfListTarifBrgResponseBase>>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IStokRepo _stokRepo;

    public TrfListTarifBrgHandler(INilaiTarifRepo nilaiTarifRepo, IStokRepo stokRepo)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _stokRepo = stokRepo;
    }

    public Task<IEnumerable<TrfListTarifBrgResponseBase>> Handle(TrfListTarifBrgQuery request, CancellationToken cancellationToken)
    {
        var listBrg = _stokRepo.ListData(request, request.Keyword)?.ToList() ?? [];
        var listTarif = _nilaiTarifRepo.ListData(request, request)?.ToList() ?? [];
        listTarif = listTarif.Where(x => x.TarifName.Contains(request.Keyword)).ToList();
        
        var brgResponse = listBrg.Select(x => new TrfListBrg(x.BrgId, x.BrgName, x.Qty, x.Satuan));
        var nilaiResponse = listTarif.Select(x => new TrfListTarif(x.TarifId, x.TarifName, x.Nilai));
        
        var result = brgResponse.Concat<TrfListTarifBrgResponseBase>(nilaiResponse);
        return Task.FromResult(result);
        
    }
}