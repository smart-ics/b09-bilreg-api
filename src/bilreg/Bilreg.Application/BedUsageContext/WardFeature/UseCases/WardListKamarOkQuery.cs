using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.WardFeature.UseCases;

public record WardListKamarOkQuery() : IRequest<IEnumerable<WardListKamarOkResponse>>;

public record WardListKamarOkResponse(
    string KamarId, string KamarName);

public class WardListKamarOkHandler : IRequestHandler<WardListKamarOkQuery, IEnumerable<WardListKamarOkResponse>>
{
    private readonly IKamarRepo _kamarRepo;
    private readonly IBangsalRepo _bangsalRepo;

    const string ROOM_CAT = "OK";

    public WardListKamarOkHandler(IKamarRepo kamarRepo, IBangsalRepo bangsalRepo)
    {
        _kamarRepo = kamarRepo;
        _bangsalRepo = bangsalRepo;
    }

    public Task<IEnumerable<WardListKamarOkResponse>> Handle(WardListKamarOkQuery request, CancellationToken cancellationToken)
    {
        //  BUILD
        var bangsal = _bangsalRepo.ListData()?
            .FirstOrDefault(x => x.RoomCat.RoomCatId == ROOM_CAT) 
            ?? BangsalType.Default;

        var listKamarOk = _kamarRepo.ListData(bangsal);
        var result = listKamarOk?
            .Select(x => new WardListKamarOkResponse(x.KamarId, x.KamarName))
            ?? new List<WardListKamarOkResponse>();
        return Task.FromResult(result);
    }
}
