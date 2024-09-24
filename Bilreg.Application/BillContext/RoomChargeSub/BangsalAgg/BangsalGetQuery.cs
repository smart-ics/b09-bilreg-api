using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;

public record BangsalGetQuery(string BangsalId) : IRequest<BangsalGetResponse>, IBangsalKey;

public record BangsalGetResponse(
    string BangsalId,
    string BangsalName,
    string LayananId,
    string LayananName);

public class BangsalGetHandler : IRequestHandler<BangsalGetQuery, BangsalGetResponse>
{
    private readonly IBangsalDal _bangsalDal;

    public BangsalGetHandler(IBangsalDal bangsalDal)
    {
        _bangsalDal = bangsalDal;
    }
    
    public Task<BangsalGetResponse> Handle(BangsalGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var bangsal = _bangsalDal.GetData(request)
            ?? throw new KeyNotFoundException($"Bangsal id {request.BangsalId} not found");

        // RESPONSE
        var response = new BangsalGetResponse(
            bangsal.BangsalId,
            bangsal.BangsalName,
            bangsal.LayananId,
            bangsal.LayananName
            );
        
        return Task.FromResult(response);

    }
}