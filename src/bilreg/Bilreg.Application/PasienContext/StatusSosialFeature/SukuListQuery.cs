using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record SukuListQuery : IRequest<IEnumerable<SukuListResponse>>;

[PublicAPI]
public record SukuListResponse(string SukuId, string SukuName);

public class SukuListHandler : IRequestHandler<SukuListQuery, IEnumerable<SukuListResponse>>
{
    private readonly ISukuDal _sukuDal;

    public SukuListHandler(ISukuDal sukuDal)
    {
        _sukuDal = sukuDal;
    }

    public Task<IEnumerable<SukuListResponse>> Handle(SukuListQuery request, CancellationToken cancellationToken)
        => _sukuDal.ListData()
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new SukuListResponse(y.SukuId, y.SukuName))),
                onNone: () => throw new KeyNotFoundException($"Suku not found"));
}