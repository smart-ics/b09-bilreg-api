using Ardalis.GuardClauses;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;

public record WardCockpitOverviewQuery(string BangsalId) : IRequest<WardCockpitOverviewDto?>;

public class WardCockpitOverviewQueryHandler : IRequestHandler<WardCockpitOverviewQuery, WardCockpitOverviewDto?>
{
    private readonly IWardCockpitDal _wardCockpitDal;
    private readonly ITglJamProvider _tglJamProvider;

    public WardCockpitOverviewQueryHandler(IWardCockpitDal wardCockpitDal, ITglJamProvider tglJamProvider)
    {
        _wardCockpitDal = wardCockpitDal;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<WardCockpitOverviewDto?> Handle(WardCockpitOverviewQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BangsalId, nameof(request.BangsalId));

        return await _wardCockpitDal.GetOverviewAsync(request.BangsalId.Trim(), _tglJamProvider.Now, cancellationToken);
    }
}
