using MediatR;

namespace Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;

public record WardCockpitLookupBabyQuery(string Keyword) : IRequest<IEnumerable<WardCockpitBabyLookupDto>>;

public class WardCockpitLookupBabyQueryHandler : IRequestHandler<WardCockpitLookupBabyQuery, IEnumerable<WardCockpitBabyLookupDto>>
{
    private readonly IWardCockpitDal _wardCockpitDal;

    public WardCockpitLookupBabyQueryHandler(IWardCockpitDal wardCockpitDal)
    {
        _wardCockpitDal = wardCockpitDal;
    }

    public async Task<IEnumerable<WardCockpitBabyLookupDto>> Handle(WardCockpitLookupBabyQuery request, CancellationToken cancellationToken)
    {
        return await _wardCockpitDal.LookupBabyAsync(request.Keyword, cancellationToken);
    }
}
