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
    {
        var tipeJaminan = _tipeJaminanDal
            .GetData(request).Value;
        
        if (tipeJaminan is null) throw new KeyNotFoundException($"Tipe Jaminan {request.TipeJaminanId} nnot found");
        
        var response = new TipeJaminanGetResponse(
            tipeJaminan.TipeJaminanId, tipeJaminan.TipeJaminanName,
            tipeJaminan.Jaminan);

        return Task.FromResult(response);
    }
}