using Ardalis.GuardClauses;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;

public record WardCockpitBedDetailQuery(string BedId) : IRequest<WardCockpitBedDetailDto?>;

public class WardCockpitBedDetailQueryHandler : IRequestHandler<WardCockpitBedDetailQuery, WardCockpitBedDetailDto?>
{
    private readonly IWardCockpitDal _wardCockpitDal;
    private readonly ITglJamProvider _tglJamProvider;

    public WardCockpitBedDetailQueryHandler(IWardCockpitDal wardCockpitDal, ITglJamProvider tglJamProvider)
    {
        _wardCockpitDal = wardCockpitDal;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<WardCockpitBedDetailDto?> Handle(WardCockpitBedDetailQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BedId, nameof(request.BedId));

        return await _wardCockpitDal.GetBedDetailAsync(request.BedId.Trim(), _tglJamProvider.Now, cancellationToken);
    }
}
