using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;

public record TipeJaminanGetQuery(string TipeJaminanId) : IRequest<TipeJaminanGetResponse>, ITipeJaminanKey;

public record TipeJaminanGetResponse(
    string TipeJaminanId,
    string TipeJaminanName,
    JaminanViewType Jaminan);

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
            .GetData2(request)
            .OrThrowNotFoundException()
            .Value;
        
        var response = new TipeJaminanGetResponse(
            tipeJaminan.TipeJaminanId, tipeJaminan.TipeJaminanName,
            tipeJaminan.Jaminan);
        
        return Task.FromResult(response);
    }
}