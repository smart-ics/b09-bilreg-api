using Bilreg.Domain.PasienContext.DemografiFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiSub;

public record KabupatenGetQuery(string KabupatenId) : IRequest<KabupatenGetResponse>;

[PublicAPI]
public record KabupatenGetResponse(string KabupatenId, string KabupatenName, PropinsiType Propinsi);

public class KabupatenGetHandler : IRequestHandler<KabupatenGetQuery, KabupatenGetResponse>
{
    private readonly IKabupatenDal _kabupatenDal;

    public KabupatenGetHandler(IKabupatenDal kabupatenDal)
    {
        _kabupatenDal = kabupatenDal;
    }

    public Task<KabupatenGetResponse> Handle(KabupatenGetQuery request, CancellationToken cancellationToken)
        =>  _kabupatenDal.GetData(KabupatenType.Key(request.KabupatenId))
            .Match(
                onSome: x => Task.FromResult(new KabupatenGetResponse(x.KabupatenId, x.KabupatenName, x.Propinsi)), 
                onNone: () => throw new KeyNotFoundException($"Kabupaten {request.KabupatenId} not found"));
}