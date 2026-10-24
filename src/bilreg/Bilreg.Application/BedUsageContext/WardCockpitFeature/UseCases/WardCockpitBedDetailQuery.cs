using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;

public record WardCockpitBedDetailQuery(string BedId) : IRequest<WardCockpitBedDetailDto?>;

public class WardCockpitBedDetailQueryHandler : IRequestHandler<WardCockpitBedDetailQuery, WardCockpitBedDetailDto?>
{
    private readonly IWardCockpitDal _wardCockpitDal;

    public WardCockpitBedDetailQueryHandler(IWardCockpitDal wardCockpitDal)
    {
        _wardCockpitDal = wardCockpitDal;
    }

    public async Task<WardCockpitBedDetailDto?> Handle(WardCockpitBedDetailQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BedId, nameof(request.BedId));

        return await _wardCockpitDal.GetBedDetailAsync(request.BedId.Trim(), cancellationToken);
    }
}
