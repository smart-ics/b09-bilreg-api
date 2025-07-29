using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;

public record TipeJaminanListQuery() : IRequest<IEnumerable<TipeJaminanListResponse>>;

public record TipeJaminanListResponse(
    string TipeJaminanId,
    string TipeJaminanName,
    string JaminanId,
    string JaminanName);

public class TipeJaminanListHandler : IRequestHandler<TipeJaminanListQuery, IEnumerable<TipeJaminanListResponse>>
{
    private readonly ITipeJaminanDal _tipeJaminanDal;

    public TipeJaminanListHandler(ITipeJaminanDal tipeJaminanDal)
    {
        _tipeJaminanDal = tipeJaminanDal;
    }

    public Task<IEnumerable<TipeJaminanListResponse>> Handle(TipeJaminanListQuery request, 
        CancellationToken cancellationToken)
        => _tipeJaminanDal.ListData()
        .Match(
            onSome: x => Task.FromResult(x.Select(y
                => new TipeJaminanListResponse(y.TipeJaminanId, y.TipeJaminanName, y.Jaminan.JaminanId, y.Jaminan.JaminanName))),
            onNone: () => throw new KeyNotFoundException($"TipeJaminan not found"));
}