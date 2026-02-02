using System.Text.Json.Serialization;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
// ReSharper disable All

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfListTarifBrgQuery(string LayananId, string KelasId, string TipeTarifId, string Keyword) 
    : IRequest<IEnumerable<TrfListTarifBrgResponseBase>>, ILayananKey, INilaiTarifVariant;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TrfListTarif), "TARIF")]
[JsonDerivedType(typeof(TrfListBrg), "BARANG")]
public abstract record TrfListTarifBrgResponseBase(string DisplayName);
public record TrfListTarif(string TarifId, string TarifName, decimal Harga) 
    : TrfListTarifBrgResponseBase(TarifName);
public record TrfListBrg(string BrgId, string BrgName, decimal Stok, string Satuan)
    : TrfListTarifBrgResponseBase(BrgName);
public class TrfListTarifBrgHandler 
    : IRequestHandler<TrfListTarifBrgQuery, IEnumerable<TrfListTarifBrgResponseBase>>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IStokRepo _stokRepo;
    public TrfListTarifBrgHandler(INilaiTarifRepo nilaiTarifRepo, IStokRepo stokRepo)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _stokRepo = stokRepo;
    }
    public Task<IEnumerable<TrfListTarifBrgResponseBase>> Handle(TrfListTarifBrgQuery request, 
        CancellationToken cancellationToken)
    {
        var listBrg = _stokRepo.ListData(request, request.Keyword)?.ToList() ?? [];
        var listTarif = _nilaiTarifRepo.Search(request, request, request.Keyword)?.ToList() ?? [];
        
        var brgResponse = listBrg.Select(x => new TrfListBrg(x.BrgId, x.BrgName, x.Qty, x.Satuan));
        var tarifResponse = listTarif.Select(x => new TrfListTarif(x.TarifId, x.TarifName, x.Nilai));
        
        var result = tarifResponse
            .Concat<TrfListTarifBrgResponseBase>(brgResponse)
            .OrderBy(x => x.DisplayName);
        
        return Task.FromResult(result.AsEnumerable());
    }
}