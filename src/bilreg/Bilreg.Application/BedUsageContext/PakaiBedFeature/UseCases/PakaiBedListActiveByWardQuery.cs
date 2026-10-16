using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;

public record PakaiBedListActiveByWardQuery(string BangsalId) : IRequest<IEnumerable<PakaiBedActiveOccupancyResponse>>, IBangsalKey;

public class PakaiBedListActiveByWardQueryHandler : IRequestHandler<PakaiBedListActiveByWardQuery, IEnumerable<PakaiBedActiveOccupancyResponse>>
{
    private readonly IPakaiBedActiveOccupancyDal _dal;

    public PakaiBedListActiveByWardQueryHandler(IPakaiBedActiveOccupancyDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<PakaiBedActiveOccupancyResponse>> Handle(PakaiBedListActiveByWardQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BangsalId))
            return Task.FromResult<IEnumerable<PakaiBedActiveOccupancyResponse>>([]);

        var result = _dal.ListActiveByWard(request.BangsalId) ?? [];
        return Task.FromResult(result);
    }
}
