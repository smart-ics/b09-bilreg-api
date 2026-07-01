using Bilreg.Domain.PasienContext.DemografiFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public record KelurahanGetQuery(string KelurahanId) : IRequest<KelurahanGetResponse>;

[PublicAPI]
public record KelurahanGetResponse(string KelurahanId, string KelurahanName, 
    KabupatenReff Kabupaten, PropinsiType Propinsi);

public class KelurahanGetHandler : IRequestHandler<KelurahanGetQuery, KelurahanGetResponse>
{
    private readonly IKelurahanRepo _kelurahanRepo;

    public KelurahanGetHandler(IKelurahanRepo kelurahanRepo)
    {
        _kelurahanRepo = kelurahanRepo;
    }

    public Task<KelurahanGetResponse> Handle(KelurahanGetQuery request, CancellationToken cancellationToken)
        =>  _kelurahanRepo.LoadEntity(KelurahanType.Key(request.KelurahanId))
            .Match(
                onSome: x => Task.FromResult(new KelurahanGetResponse(x.KelurahanId, x.KelurahanName,
                    x.Kabupaten, x.Propinsi)), 
                onNone: () => throw new KeyNotFoundException($"Kelurahan {request.KelurahanId} not found"));
}