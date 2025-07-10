using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;

public record TipeJaminanListQuery() : IRequest<IEnumerable<TipeJaminanListResponse>>;

public record TipeJaminanListResponse(
    string TipeJaminanId,
    string TipeJaminanName,
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
            .ListData2()
            .Value;

        var response = listTipeJaminan
            .OrderBy(x => x.TipeJaminanId)
            .Select(x => new TipeJaminanListResponse(
                x.TipeJaminanId,
                x.TipeJaminanName,
                x.Jaminan.JaminanName));
        
        return Task.FromResult(response);
    }
}