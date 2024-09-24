using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public record BangsalListQuery() : IRequest<IEnumerable<BangsalListResponse>>;

public record BangsalListResponse(
    string BangsalId,
    string BangsalName,
    string LayananId,
    string LayananName);
    
public class BangsalListHandler : IRequestHandler<BangsalListQuery, IEnumerable<BangsalListResponse>>
{
    private readonly IBangsalDal _bangsalDal;

    public BangsalListHandler(IBangsalDal bangsalDal)
    {
        _bangsalDal = bangsalDal;
    }
    
    public Task<IEnumerable<BangsalListResponse>> Handle(BangsalListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var bangsalList = _bangsalDal.ListData()
            ?? throw new KeyNotFoundException("BangsalList not found");
        
        // RESPONSE
        var response = bangsalList.Select(x => new BangsalListResponse(
            x.BangsalId,
            x.BangsalName,
            x.LayananId,
            x.LayananName));
        return Task.FromResult(response);
    }
}