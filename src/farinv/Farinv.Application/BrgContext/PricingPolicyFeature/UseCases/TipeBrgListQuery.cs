using MediatR;

namespace Farinv.Application.BrgContext.PricingPolicyFeature.UseCases;

public record TipeBrgListQuery() : IRequest<IEnumerable<TipeBrgListResponse>>;

public record TipeBrgListResponse(string TipeBrgId, string TipeBrgName, bool IsActive,
    decimal BiayaPerBarang, decimal BiayaPerRacik, decimal Profit, decimal Tax, decimal Diskon);

public class TipeBrgListHandler : IRequestHandler<TipeBrgListQuery, IEnumerable<TipeBrgListResponse>>
{
    private readonly ITipeBrgRepo _tipeBrgRepo;

    public TipeBrgListHandler(ITipeBrgRepo tipeBrgRepo)
    {
        _tipeBrgRepo = tipeBrgRepo;
    }

    public Task<IEnumerable<TipeBrgListResponse>> Handle(TipeBrgListQuery request, CancellationToken cancellationToken)
    {
        var listFormularium = _tipeBrgRepo.ListData()?.ToList() ?? [];
        var response = listFormularium
            .OrderBy(x => x.TipeBrgName)
            .Select(x => new TipeBrgListResponse(x.TipeBrgId, x.TipeBrgName, x.IsActive, 
            x.BiayaPerBarang, x.BiayaPerRacik, x.Profit, x.Tax, x.Diskon));

        return Task.FromResult(response);
    }
}