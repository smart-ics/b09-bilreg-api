using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;

public record TipeJaminanSearchQuery(string Keyword) : IRequest<IEnumerable<TipeJaminanSearchResponse>>;

public record TipeJaminanSearchResponse(string TipeJaminanId,
    string TipeJaminanName,
    string JaminanId,
    string JaminanName);


public class TipeJaminanSearchHandler : IRequestHandler<TipeJaminanSearchQuery, IEnumerable<TipeJaminanSearchResponse>>
{
    private readonly ITipeJaminanDal _dal;

    public TipeJaminanSearchHandler(ITipeJaminanDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<TipeJaminanSearchResponse>> Handle(TipeJaminanSearchQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));
        if (request.Keyword.Length < 2)
            throw new ArgumentException("Keyword terlalu pendek (minimal 2 karakter).");

        var listTipeJaminan = _dal.ListData()
            .Match(
                some => some,
                () => throw new ArgumentException("data not found"));
        var result = listTipeJaminan.ToList()
            .Where(x => x.TipeJaminanName.ToLower().Contains(request.Keyword.ToLower()))
            .Select(x => new TipeJaminanSearchResponse(x.TipeJaminanId, x.TipeJaminanName,
                x.Jaminan.JaminanId, x.Jaminan.JaminanName));

        return Task.FromResult(result);
    }
}