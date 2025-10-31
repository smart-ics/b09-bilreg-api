using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public record KotaGetQuery(string KotaId) : IRequest<KotaGetResponse>;

[PublicAPI]
public record KotaGetResponse(string KotaId, string KotaName);

public class KotaGetHandler : IRequestHandler<KotaGetQuery, KotaGetResponse>
{
    private readonly IKotaDal _kotaDal;

    public KotaGetHandler(IKotaDal kotaDal)
    {
        _kotaDal = kotaDal;
    }

    public Task<KotaGetResponse> Handle(KotaGetQuery request, CancellationToken cancellationToken)
        =>  _kotaDal.GetData(KotaType.Key(request.KotaId))
            .Match(
                onSome: x => Task.FromResult(new KotaGetResponse(x.KotaId, x.KotaName)), 
                onNone: () => throw new KeyNotFoundException($"Kota {request.KotaId} not found"));
}