using MediatR;

namespace Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;

public record WardCockpitBangsalListQuery : IRequest<IEnumerable<WardCockpitBangsalListItemDto>>;

public class WardCockpitBangsalListQueryHandler : IRequestHandler<WardCockpitBangsalListQuery, IEnumerable<WardCockpitBangsalListItemDto>>
{
    private readonly IWardCockpitDal _wardCockpitDal;

    public WardCockpitBangsalListQueryHandler(IWardCockpitDal wardCockpitDal)
    {
        _wardCockpitDal = wardCockpitDal;
    }

    public async Task<IEnumerable<WardCockpitBangsalListItemDto>> Handle(WardCockpitBangsalListQuery request, CancellationToken cancellationToken)
    {
        return await _wardCockpitDal.ListActiveBangsalAsync(cancellationToken);
    }
}
