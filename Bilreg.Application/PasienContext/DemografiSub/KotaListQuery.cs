using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiSub;

public record KotaListQuery : IRequest<IEnumerable<KotaListResponse>>;

[PublicAPI]
public record KotaListResponse(string KotaId, string KotaName);

public class KotaListHandler : IRequestHandler<KotaListQuery, IEnumerable<KotaListResponse>>
{
    private readonly IKotaDal _kotaDal;

    public KotaListHandler(IKotaDal kotaDal)
    {
        _kotaDal = kotaDal;
    }

    public Task<IEnumerable<KotaListResponse>> Handle(KotaListQuery request, CancellationToken cancellationToken)
        => _kotaDal.ListData()
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new KotaListResponse(y.KotaId, y.KotaName))),
                onNone: () => throw new KeyNotFoundException($"Kota not found"));
}