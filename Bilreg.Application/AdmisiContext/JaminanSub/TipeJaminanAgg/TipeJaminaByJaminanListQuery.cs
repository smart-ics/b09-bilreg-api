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

    public Task<IEnumerable<TipeJaminanByJaminanListResponse>> Handle(TipeJaminaByJaminanListQuery request,
        CancellationToken cancellationToken)
        => _tipeJaminanDal.ListData(request)
        .Match(
            onSome: x => Task.FromResult(x.Select(y
                => new TipeJaminanByJaminanListResponse(y.TipeJaminanId, y.TipeJaminanName, y.Jaminan.JaminanId, y.Jaminan.JaminanName))),
            onNone: () => throw new KeyNotFoundException($"TipeJaminan by Jaminan {request.JaminanId} not found"));
}
