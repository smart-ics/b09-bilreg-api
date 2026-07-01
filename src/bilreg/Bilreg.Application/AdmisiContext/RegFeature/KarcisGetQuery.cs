using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record KarcisGetQuery(string KarcisId) : IRequest<KarcisGetResponse>, IKarcisKey;

public record KarcisGetResponse(
    string KarcisId, string KarcisName, bool IsAktif,
    InstalasiDkType InstalasiDk, RekapCetakReff RekapCetak,
    TarifReff DefaultTarif,
    IEnumerable<KarcisKomponenType> ListKomponen,
    IEnumerable<LayananReff> ListLayanan,
    decimal NilaiKarcis);

public class KarcisGetHandler : IRequestHandler<KarcisGetQuery, KarcisGetResponse>
{
    private readonly IKarcisRepo _karcisRepo;

    public KarcisGetHandler(IKarcisRepo karcisRepo)
    {
        _karcisRepo = karcisRepo;
    }

    public Task<KarcisGetResponse> Handle(KarcisGetQuery request, CancellationToken cancellationToken)
    {
        var karcis = _karcisRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"karcis {request.KarcisId} not found")
            );

        var result = new KarcisGetResponse(
            karcis.KarcisId, karcis.KarcisName,
            karcis.IsAktif, karcis.InstalasiDk,
            karcis.RekapCetak, karcis.DefaultTarif,
            karcis.ListKomponen, karcis.ListLayanan,
            karcis.NilaiKarcis);

        return Task.FromResult(result);
    }
}
