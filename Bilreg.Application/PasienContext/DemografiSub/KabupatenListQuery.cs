using Bilreg.Domain.PasienContext.DemografiFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiSub;

public record KabupatenListQuery(string PropinsiId) : IRequest<IEnumerable<KabupatenListResponse>>;

[PublicAPI]
public record KabupatenListResponse(string KabupatenId, string KabupatenName, PropinsiType Propinsi);

public class KabupatenListHandler : IRequestHandler<KabupatenListQuery, IEnumerable<KabupatenListResponse>>
{
    private readonly IKabupatenDal _kabupatenDal;

    public KabupatenListHandler(IKabupatenDal kabupatenDal)
    {
        _kabupatenDal = kabupatenDal;
    }

    public Task<IEnumerable<KabupatenListResponse>> Handle(KabupatenListQuery request, CancellationToken cancellationToken)
        => _kabupatenDal.ListData(PropinsiType.Key(request.PropinsiId))
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new KabupatenListResponse(y.KabupatenId, y.KabupatenName, y.Propinsi))),
                onNone: () => throw new KeyNotFoundException($"Kabupaten not found"));
}