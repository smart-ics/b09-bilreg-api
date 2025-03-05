using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;

public record SukuListQuery() : IRequest<IEnumerable<SukuListResponse>>;

public record SukuListResponse(string SukuId, string SukuName);

public class SukuListHandler : IRequestHandler<SukuListQuery, IEnumerable<SukuListResponse>>
{
    private readonly ISukuDal _sukuDal;

    public SukuListHandler(ISukuDal sukuDal)
    {
        _sukuDal = sukuDal;
    }

    public Task<IEnumerable<SukuListResponse>> Handle(SukuListQuery request, CancellationToken cancellationToken)
    {
        //  QUERY
        var result = _sukuDal.ListData2().Value;

        //  RESPONSE
        var response = result.Select(x => new SukuListResponse(x.SukuId, x.SukuName));
        return Task.FromResult(response);
    }
}