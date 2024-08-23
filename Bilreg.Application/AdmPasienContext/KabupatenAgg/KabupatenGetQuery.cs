using Bilreg.Domain.AdmPasienContext.KabupatenAgg;
using MediatR;

namespace Bilreg.Application.AdmPasienContext.KabupatenAgg;

public record KabupatenGetQuery(string KabupatenId) : IRequest<KabupatenGetResponse>, IKabupatenKey;

public record KabupatenGetResponse(
    string KabupatenId,
    string KabupatenName,
    string PropinsiId,
    string PropinsiName);

public class KabupatenGetHandler : IRequestHandler<KabupatenGetQuery, KabupatenGetResponse>
{
    private readonly IKabupatenDal _kabupatenDal;

    public KabupatenGetHandler(IKabupatenDal kabupatenDal)
    {
        _kabupatenDal = kabupatenDal;
    }

    public Task<KabupatenGetResponse> Handle(KabupatenGetQuery request, CancellationToken cancellationToken)
    {
        var result = _kabupatenDal.GetData(request)
            ?? throw new KeyNotFoundException($"KabupatenId not found: {request.KabupatenId}");
        var response = new KabupatenGetResponse(
            result.KabupatenId, result.KabupatenName, result.PropinsiId, result.PropinsiName);
        return Task.FromResult(response);
    }
}
