using Bilreg.Domain.PasienContext.StatusSosialFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;

public record SukuGetQuery(string SukuId) : IRequest<SukuGetResponse>;

[PublicAPI]
public record SukuGetResponse(string SukuId, string SukuName);

public class SukuGetHandler : IRequestHandler<SukuGetQuery, SukuGetResponse>
{
    private readonly ISukuDal _sukuDal;

    public SukuGetHandler(ISukuDal sukuDal)
    {
        _sukuDal = sukuDal;
    }

    public Task<SukuGetResponse> Handle(SukuGetQuery request, CancellationToken cancellationToken)
    =>  _sukuDal.GetData(SukuType.Key(request.SukuId))
            .Match(
                onSome: x => Task.FromResult(new SukuGetResponse(x.SukuId, x.SukuName)), 
                onNone: () => throw new KeyNotFoundException($"Suku {request.SukuId} not found"));
}