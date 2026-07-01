using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public record TipeJaminanSearchQeury(string Keyword) : IRequest<IEnumerable<TipeJaminanView>>;

public class TipeJaminanSearchHandler : IRequestHandler<TipeJaminanSearchQeury, IEnumerable<TipeJaminanView>>
{
    private readonly ITipeJaminanRepo _tipeJaminanRepo;

    public TipeJaminanSearchHandler(ITipeJaminanRepo tipeJaminanRepo)
    {
        _tipeJaminanRepo = tipeJaminanRepo;
    }

    public Task<IEnumerable<TipeJaminanView>> Handle(TipeJaminanSearchQeury request, CancellationToken cancellationToken)
    {
        var listTipeJmn = _tipeJaminanRepo.ListData();
        var listResult = listTipeJmn
            .Where(x => x.TipeJaminanName.ToLower().Contains(request.Keyword.ToLower()))?.ToList()
            ?? [];

        return Task.FromResult(listResult.AsEnumerable());
    }
}
