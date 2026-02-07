using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record KarcisListByLayananQuery(string LayananId) : IRequest<IEnumerable<KarcisListByLayananResponse>>, ILayananKey;

public record KarcisListByLayananResponse(string KarcisId, string KarcisName,
    LayananReff Layanan,
    TarifReff DefaultTarif,
    decimal Nilai);


public class KarcisListByLayananHandler : IRequestHandler<KarcisListByLayananQuery, IEnumerable<KarcisListByLayananResponse>>
{
    private readonly IKarcisRepo _karcisRepo;

    public KarcisListByLayananHandler(IKarcisRepo karcisRepo)
    {
        _karcisRepo = karcisRepo;
    }

    public Task<IEnumerable<KarcisListByLayananResponse>> Handle(KarcisListByLayananQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananId);

        var listKarcis = _karcisRepo.ListData(request)?.ToList() ?? [];
        var response = listKarcis.Select(x => new KarcisListByLayananResponse(
            x.KarcisId, x.KarcisName, x.Layanan, x.DefaultTarif, x.Nilai));

        return Task.FromResult(response);
    }
}