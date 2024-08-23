using Bilreg.Domain.AdmPasienContext.PropinsiAgg;
using MediatR;

namespace Bilreg.Application.AdmPasienContext.KabupatenAgg;

public record KabupatenListQuery(string PropinsiId) 
    : IRequest<IEnumerable<KabupatenListResponse>>, IPropinsiKey;

public record KabupatenListResponse(string KabupatenId, string KabupatenName, 
    string PropinsiId, string PropinsiName);

public class KabuaptenListHandler : IRequestHandler<KabupatenListQuery, IEnumerable<KabupatenListResponse>>
{
    private readonly IKabupatenDal _kabupatenDal;

    public KabuaptenListHandler(IKabupatenDal kabupatenDal)
    {
        _kabupatenDal = kabupatenDal;
    }

    public Task<IEnumerable<KabupatenListResponse>> Handle(KabupatenListQuery request, CancellationToken cancellationToken)
    {
        var listKab = _kabupatenDal.ListData(request)
            ?? throw new KeyNotFoundException($"Kabupaten not founld");
        var response = listKab.Select(x => new KabupatenListResponse(
            x.KabupatenId, x.KabupatenName, x.PropinsiId, x.PropinsiName));
        return Task.FromResult(response);
    }
}