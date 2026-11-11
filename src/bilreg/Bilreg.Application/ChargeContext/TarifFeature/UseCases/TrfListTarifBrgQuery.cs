using System.Text.Json.Serialization;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
// ReSharper disable All

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfListTarifBrgQuery(string LayananId, string KelasId, string TipeTarifId, string Keyword, string TipeJaminanId = "") 
    : IRequest<IEnumerable<TrfListTarifBrgResponseBase>>, ILayananKey, INilaiTarifVariant;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TrfListTarif), "TARIF")]
[JsonDerivedType(typeof(TrfListBrg), "BARANG")]
public abstract record TrfListTarifBrgResponseBase(string DisplayName);

public record TrfListTarif(string TarifId, string TarifName, decimal Harga) 
    : TrfListTarifBrgResponseBase(TarifName);

public record TrfListBrg(string BrgId, string BrgName, decimal Stok, string SatuanId, string SatuanName, decimal Harga = 0)
    : TrfListTarifBrgResponseBase(BrgName)
{
    public string Satuan => SatuanName;
}

public class TrfListTarifBrgHandler 
    : IRequestHandler<TrfListTarifBrgQuery, IEnumerable<TrfListTarifBrgResponseBase>>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IStokRepo _stokRepo;
    private readonly IBrgPricingService _brgPricingService;

    public TrfListTarifBrgHandler(INilaiTarifRepo nilaiTarifRepo, IStokRepo stokRepo, IBrgPricingService brgPricingService)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _stokRepo = stokRepo;
        _brgPricingService = brgPricingService;
    }
    
    public Task<IEnumerable<TrfListTarifBrgResponseBase>> Handle(TrfListTarifBrgQuery request, 
        CancellationToken cancellationToken)
    {
        var listBrg = _stokRepo.ListData(request, request.Keyword)?.ToList() ?? [];
        var listTarif = _nilaiTarifRepo.Search(request, request, request.Keyword)?.ToList() ?? [];

        var brgIds = listBrg.Select(x => x.BrgId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var prices = brgIds.Count > 0
            ? _brgPricingService.GetUnitPrices(brgIds, request.TipeJaminanId, request.LayananId)
            : new Dictionary<string, decimal>();

        var brgResponse = listBrg.Select(x =>
        {
            var harga = prices.TryGetValue(x.BrgId, out var p) ? p : 0m;
            return new TrfListBrg(x.BrgId, x.BrgName, x.Qty, x.SatuanId, x.SatuanName, harga);
        });
        
        var tarifResponse = listTarif.Select(x => new TrfListTarif(x.TarifId, x.TarifName, x.Nilai));
        
        var result = tarifResponse
            .Concat<TrfListTarifBrgResponseBase>(brgResponse)
            .OrderBy(x => x.DisplayName);
        
        return Task.FromResult(result.AsEnumerable());
    }
}