using Bilreg.Domain.PasienContext.DemografiFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiSub;

public record KecamatanGetQuery(string KecamatanId) : IRequest<KecamatanGetResponse>;

[PublicAPI]
public record KecamatanGetResponse(string KecamatanId, string KecamatanName, 
    KabupatenReff Kabupaten, PropinsiType Propinsi);

public class KecamatanGetHandler : IRequestHandler<KecamatanGetQuery, KecamatanGetResponse>
{
    private readonly IKecamatanDal _kecamatanDal;

    public KecamatanGetHandler(IKecamatanDal kecamatanDal)
    {
        _kecamatanDal = kecamatanDal;
    }

    public Task<KecamatanGetResponse> Handle(KecamatanGetQuery request, CancellationToken cancellationToken)
        =>  _kecamatanDal.GetData(KecamatanType.Key(request.KecamatanId))
            .Match(
                onSome: x => Task.FromResult(new KecamatanGetResponse(x.KecamatanId, x.KecamatanName,
                    x.Kabupaten, x.Propinsi)), 
                onNone: () => throw new KeyNotFoundException($"Kecamatan {request.KecamatanId} not found"));
}