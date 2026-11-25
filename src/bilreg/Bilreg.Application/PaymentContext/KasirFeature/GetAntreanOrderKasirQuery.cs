using Ardalis.GuardClauses;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record GetAntreanOrderKasirQuery(string? Keyword = null, string? Date = null) : IRequest<IEnumerable<OrderKasirItemDto>>;

public class GetAntreanOrderKasirHandler : IRequestHandler<GetAntreanOrderKasirQuery, IEnumerable<OrderKasirItemDto>>
{
    private readonly IKasirOrderInboxDal _inboxDal;
    private readonly ITglJamProvider _tglJamProvider;

    public GetAntreanOrderKasirHandler(IKasirOrderInboxDal inboxDal, ITglJamProvider tglJamProvider)
    {
        _inboxDal = inboxDal;
        _tglJamProvider = tglJamProvider;
    }

    public Task<IEnumerable<OrderKasirItemDto>> Handle(GetAntreanOrderKasirQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        var targetDate = !string.IsNullOrWhiteSpace(request.Date)
            ? request.Date.Trim()
            : _tglJamProvider.Now.ToString("yyyy-MM-dd");

        var items = _inboxDal.ListOrderInbox(targetDate) ?? Enumerable.Empty<OrderKasirItemDto>();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            items = items.Where(x =>
                (x.OrderId != null && x.OrderId.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (x.RegId != null && x.RegId.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (x.NoRm != null && x.NoRm.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (x.PasienName != null && x.PasienName.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (x.Subtype != null && x.Subtype.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (x.UnitName != null && x.UnitName.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (x.PenjaminName != null && x.PenjaminName.Contains(kw, StringComparison.OrdinalIgnoreCase)));
        }

        return Task.FromResult(items);
    }
}
