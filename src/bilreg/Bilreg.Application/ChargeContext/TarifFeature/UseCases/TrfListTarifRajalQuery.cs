using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfListTarifRajalQuery(string LayananId, string KelasId, string TipeTarifId, string Keyword)
    : IRequest<IEnumerable<TrfListTarif>>, ILayananKey, INilaiTarifVariant;

public class TrfListTarifRajalHandler : IRequestHandler<TrfListTarifRajalQuery, IEnumerable<TrfListTarif>>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;

    public TrfListTarifRajalHandler(INilaiTarifRepo nilaiTarifRepo)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
    }

    public Task<IEnumerable<TrfListTarif>> Handle(TrfListTarifRajalQuery request, CancellationToken cancellationToken)
    {
        var listTarif = _nilaiTarifRepo.Search(request, request, request.Keyword)?.ToList() ?? [];
        var result = listTarif
            .Select(x => new TrfListTarif(x.TarifId, x.TarifName, x.Nilai))
            .OrderBy(x => x.TarifName);

        return Task.FromResult(result.AsEnumerable());
    }
}
