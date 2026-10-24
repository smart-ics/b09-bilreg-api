using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;

public record WardCockpitOverviewQuery(string BangsalId) : IRequest<WardCockpitOverviewDto?>;

public class WardCockpitOverviewQueryHandler : IRequestHandler<WardCockpitOverviewQuery, WardCockpitOverviewDto?>
{
    private readonly IWardCockpitDal _wardCockpitDal;

    public WardCockpitOverviewQueryHandler(IWardCockpitDal wardCockpitDal)
    {
        _wardCockpitDal = wardCockpitDal;
    }

    public async Task<WardCockpitOverviewDto?> Handle(WardCockpitOverviewQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BangsalId, nameof(request.BangsalId));

        return await _wardCockpitDal.GetOverviewAsync(request.BangsalId.Trim(), cancellationToken);
    }
}
