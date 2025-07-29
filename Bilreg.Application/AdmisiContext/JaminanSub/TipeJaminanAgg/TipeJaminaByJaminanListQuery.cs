using Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;

public record TipeJaminaByJaminanListQuery(string JaminanId) : 
    IRequest<IEnumerable<TipeJaminanByJaminanListResponse>>, IJaminanKey;

public record TipeJaminanByJaminanListResponse(
string TipeJaminanId,
string TipeJaminanName,
string JaminanId,
string JaminanName);

public class TipeJaminaByJaminanListHandler : IRequestHandler<TipeJaminaByJaminanListQuery,
    IEnumerable<TipeJaminanByJaminanListResponse>>
{
    private readonly ITipeJaminanDal _tipeJaminanDal;

    public TipeJaminaByJaminanListHandler(ITipeJaminanDal tipeJaminanDal)
    {
        _tipeJaminanDal = tipeJaminanDal;
    }

    public Task<IEnumerable<TipeJaminanByJaminanListResponse>> Handle(TipeJaminaByJaminanListQuery request, CancellationToken cancellationToken)
    {
        var listTipeJaminan = _tipeJaminanDal
            .ListData(request).Value;

        if (listTipeJaminan is null) 
            throw new KeyNotFoundException($"TipeJaminan by Jaminan {request.JaminanId} not found");

        var response = listTipeJaminan
            .OrderBy(x => x.TipeJaminanId)
            .Select(x => new TipeJaminanByJaminanListResponse(
                x.TipeJaminanId,
                x.TipeJaminanName,
                x.Jaminan.JaminanId,
                x.Jaminan.JaminanName));

        return Task.FromResult(response);
    }
}