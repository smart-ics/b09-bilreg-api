using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.TarifAgg;

public record TarifSearchQuery(string Keyword) : IRequest<IEnumerable<TarifSearchRespone>>;

public record TarifSearchRespone(string TarifId, string TarifName);

public class TarifSearchHandler : IRequestHandler<TarifSearchQuery, IEnumerable<TarifSearchRespone>>
{
    private readonly ITarifRepo _tarifRepo;

    public TarifSearchHandler(ITarifRepo tarifRepo)
    {
        _tarifRepo = tarifRepo;
    }

    public Task<IEnumerable<TarifSearchRespone>> Handle(TarifSearchQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));

        var tarifs = _tarifRepo.ListData(request.Keyword)?.ToList() ?? [];
        var result = tarifs.Select(x => new TarifSearchRespone(x.TarifId, x.TarifName));

        return Task.FromResult(result);
    }
}
