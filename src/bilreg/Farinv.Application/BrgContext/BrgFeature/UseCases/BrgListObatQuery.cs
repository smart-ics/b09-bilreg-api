using MediatR;

namespace Farinv.Application.BrgContext.BrgFeature.UseCases;

public record BrgListObatQuery(string Keyword) : IRequest<IEnumerable<BrgListObatResponse>>;

public record BrgListObatResponse(string BrgId, string BrgName);

public class BrgListObatHandler : IRequestHandler<BrgListObatQuery, IEnumerable<BrgListObatResponse>>
{
    private readonly IBrgRepo _brgRepo;

    public BrgListObatHandler(IBrgRepo brgRepo)
    {
        _brgRepo = brgRepo;
    }

    public Task<IEnumerable<BrgListObatResponse>> Handle(BrgListObatQuery request, CancellationToken cancellationToken)
    {
        var listView = _brgRepo.ListData(request.Keyword)?.ToList() ?? [];
        var result = listView
            .Where(x => x.GroupRekDkId == "OBT")
            .Select(x => new BrgListObatResponse(x.BrgId, x.BrgName));
        return Task.FromResult(result);
    }
}