using Bilreg.Domain.AdmisiContext.JaminanSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;

public record TipeJaminanGetQuery(string TipeJaminanId) : IRequest<TipeJaminanGetResponse>, ITipeJaminanKey;

public record TipeJaminanGetResponse(
    string TipeJaminanId,
    string TipeJaminanName,
    JaminanReff Jaminan);

public class TipeJaminanGetHandler : IRequestHandler<TipeJaminanGetQuery, TipeJaminanGetResponse>
{
    private readonly ITipeJaminanDal _tipeJaminanDal;

    public TipeJaminanGetHandler(ITipeJaminanDal tipeJaminanDal)
    {
        _tipeJaminanDal = tipeJaminanDal;
    }

    public Task<TipeJaminanGetResponse> Handle(TipeJaminanGetQuery request, CancellationToken cancellationToken)
        => _tipeJaminanDal.GetData(request)
        .Match(
            onSome: x => Task.FromResult(new TipeJaminanGetResponse(x.TipeJaminanId, x.TipeJaminanName, x.Jaminan)), 
            onNone: () => throw new KeyNotFoundException($"TipeJaminan {request.TipeJaminanId} not found"));
   
}