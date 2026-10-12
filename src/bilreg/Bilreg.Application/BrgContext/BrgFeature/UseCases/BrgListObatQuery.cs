using MediatR;

namespace Bilreg.Application.BrgContext.BrgFeature.UseCases;

[Obsolete("Superseded by BrgSearchQuery")]
public record BrgListObatQuery(string Keyword) : IRequest<IEnumerable<BrgListObatResponse>>;

public record BrgListObatResponse(string BrgId, string BrgName);

[Obsolete("Superseded by BrgSearchHandler")]
public class BrgListObatHandler : IRequestHandler<BrgListObatQuery, IEnumerable<BrgListObatResponse>>
{
    private readonly IBrgRepo _brgRepo;

    public BrgListObatHandler(IBrgRepo brgRepo)
    {
        _brgRepo = brgRepo;
    }

    public Task<IEnumerable<BrgListObatResponse>> Handle(BrgListObatQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            return Task.FromResult(Enumerable.Empty<BrgListObatResponse>());
        }

        var listView = _brgRepo.ListData(request.Keyword)?.ToList() ?? [];
        var result = listView
            .Where(x => x.IsAktif && string.Equals(x.GroupRekDkId, "OBT", StringComparison.OrdinalIgnoreCase))
            .Select(x => new BrgListObatResponse(x.BrgId, x.BrgName));
        return Task.FromResult(result);
    }
}