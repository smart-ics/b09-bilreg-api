
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

    public Task<IEnumerable<TipeJaminanListResponse>> Handle(TipeJaminanListQuery request, CancellationToken cancellationToken)
    {
        var listTipeJaminan = _tipeJaminanDal
            .ListData().Value;

        if (listTipeJaminan is null) throw new KeyNotFoundException("data not found");

        var response = listTipeJaminan
            .OrderBy(x => x.TipeJaminanId)
            .Select(x => new TipeJaminanListResponse(
                x.TipeJaminanId,
                x.TipeJaminanName,
                x.Jaminan.JaminanId,
                x.Jaminan.JaminanName));

        return Task.FromResult(response);
    }
}