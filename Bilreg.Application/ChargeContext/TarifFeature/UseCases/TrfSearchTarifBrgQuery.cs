using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;
using System.Text.Json.Serialization;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfSearchTarifBrgQuery(string LayananId, string Keyword) 
    : IRequest<IEnumerable<TrfSearchTarifBrgResponseBase>>, ILayananKey;
#region RESPONSE
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TrfListTarfiResp), "TARIF")]
[JsonDerivedType(typeof(TrfListBrgResp), "BARANG")]
public record TrfSearchTarifBrgResponseBase(string DisplayName);
public record TrfListTarfiResp(string TarifId, string TarifName) 
    : TrfSearchTarifBrgResponseBase(TarifName);
public record TrfListBrgResp(string BrgId, string BrgName)
    : TrfSearchTarifBrgResponseBase(BrgName);
#endregion
public class TrfSearchTarifBrgHandler : IRequestHandler<TrfSearchTarifBrgQuery, IEnumerable<TrfSearchTarifBrgResponseBase>>
{
    private readonly ITarifRepo _tarifRepo;
    private readonly IStokRepo _stokRepo;
    public TrfSearchTarifBrgHandler(ITarifRepo tarifRepo, 
        IStokRepo stokRepo)
    {
        _tarifRepo = tarifRepo;
        _stokRepo = stokRepo;
    }

    public Task<IEnumerable<TrfSearchTarifBrgResponseBase>> Handle(TrfSearchTarifBrgQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request.LayananId, nameof(request.LayananId));

        var listBrg = _stokRepo.ListData(request, request.Keyword)?.ToList() ?? [];
        var listTarif = _tarifRepo.ListData(request.Keyword)?.ToList() ?? [];

        var brgResponse = listBrg.Select(x => new TrfListBrgResp(x.BrgId, x.BrgName));
        var tarifResponse = listTarif.Select(x => new TrfListTarfiResp(x.TarifId, x.TarifName));

        var result = tarifResponse
            .Concat<TrfSearchTarifBrgResponseBase>(brgResponse)
            .OrderBy(x => x.DisplayName);

        return Task.FromResult(result.AsEnumerable());
    }
}
