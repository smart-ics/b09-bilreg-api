using Bilreg.Domain.AdmisiContext.JaminanFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public record TipeJaminanGetQuery(string TipeJaminanId) : IRequest<TipeJaminanType>, ITipeJaminanKey;

public class TipeJaminanGetHandler : IRequestHandler<TipeJaminanGetQuery, TipeJaminanType>
{
    private readonly ITipeJaminanRepo _tipeJaminanRepo;

    public TipeJaminanGetHandler(ITipeJaminanRepo tipeJaminanRepo)
    {
        _tipeJaminanRepo = tipeJaminanRepo;
    }

    public Task<TipeJaminanType> Handle(TipeJaminanGetQuery request, CancellationToken cancellationToken)
    {
        var tipeJaminan = _tipeJaminanRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tipe jaminan {request.TipeJaminanId} not found")
            );
        return Task.FromResult(tipeJaminan);
    }
}
