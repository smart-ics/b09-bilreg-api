using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.RoomRateFeature.UseCases;

public record RoomRateGetQuery(string KamarId) : IRequest<IRoomRate<IRoomRateDetail>>;

public class RoomRateGetQueryHandler : IRequestHandler<RoomRateGetQuery, IRoomRate<IRoomRateDetail>>
{
    private readonly IRoomRateRepo _roomRateRepo;

    public RoomRateGetQueryHandler(IRoomRateRepo roomRateRepo)
    {
        _roomRateRepo = roomRateRepo;
    }

    public Task<IRoomRate<IRoomRateDetail>> Handle(RoomRateGetQuery request, CancellationToken cancellationToken)
    {
        var roomRate = _roomRateRepo
            .LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow("Invalid KamarId");

        return Task.FromResult(roomRate); 
    }
}
