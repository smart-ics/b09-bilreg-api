using MediatR;

namespace Farinv.Application.BrgContext.StandardFeature.UseCases;

public record KfaSearchQuery(string Keyword) : IRequest<IEnumerable<KfaSearchResponse>>;

public record KfaSearchResponse(string Kfaid, string KfaName);

public class KfaSearchHandler
    : IRequestHandler<KfaSearchQuery, IEnumerable<KfaSearchResponse>>
{
    private readonly IKfaRepo _kfaRepo;

    public KfaSearchHandler(IKfaRepo kfaRepo)
    {
        _kfaRepo = kfaRepo; 
    }

    public Task<IEnumerable<KfaSearchResponse>> Handle(KfaSearchQuery request, 
        CancellationToken cancellationToken)
    {
        var listKfa = _kfaRepo.ListData(request.Keyword)?.ToList() ?? [];
        var response = listKfa
            .OrderBy(x => x.KfaName)
            .Select(x => new KfaSearchResponse(x.KfaId, x.KfaName));

        return Task.FromResult(response);
    }
}