using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;

public record PakaiBedGetActiveByBedQuery(string BedId) : IRequest<PakaiBedActiveOccupancyResponse?>, IBedKey;

public class PakaiBedGetActiveByBedQueryHandler : IRequestHandler<PakaiBedGetActiveByBedQuery, PakaiBedActiveOccupancyResponse?>
{
    private readonly IPakaiBedActiveOccupancyDal _dal;

    public PakaiBedGetActiveByBedQueryHandler(IPakaiBedActiveOccupancyDal dal)
    {
        _dal = dal;
    }

    public Task<PakaiBedActiveOccupancyResponse?> Handle(PakaiBedGetActiveByBedQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BedId))
            return Task.FromResult<PakaiBedActiveOccupancyResponse?>(null);

        var result = _dal.GetActiveByBed(request.BedId);
        return Task.FromResult(result);
    }
}
