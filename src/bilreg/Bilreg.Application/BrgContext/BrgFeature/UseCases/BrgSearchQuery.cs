using Bilreg.Domain.BrgContext.BrgFeature;
using MediatR;

namespace Bilreg.Application.BrgContext.BrgFeature.UseCases;

public record BrgSearchQuery(
    string Keyword,
    string? GroupRekDk = null,
    bool OnlyActive = true
) : IRequest<IEnumerable<BrgSearchResponse>>
{
    public string? GroupRekDkId => GroupRekDk;
}

public record BrgSearchResponse(
    string BrgId,
    string BrgName,
    string KetBarang,
    string GroupRekDkId,
    bool IsAktif
);

public class BrgSearchHandler : IRequestHandler<BrgSearchQuery, IEnumerable<BrgSearchResponse>>
{
    private readonly IBrgRepo _brgRepo;

    public BrgSearchHandler(IBrgRepo brgRepo)
    {
        _brgRepo = brgRepo;
    }

    public Task<IEnumerable<BrgSearchResponse>> Handle(BrgSearchQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            return Task.FromResult(Enumerable.Empty<BrgSearchResponse>());
        }

        var listView = _brgRepo.ListData(request.Keyword)?.ToList() ?? [];
        var query = listView.AsEnumerable();

        if (request.OnlyActive)
        {
            query = query.Where(x => x.IsAktif);
        }

        if (!string.IsNullOrWhiteSpace(request.GroupRekDk))
        {
            query = query.Where(x => string.Equals(x.GroupRekDkId, request.GroupRekDk, StringComparison.OrdinalIgnoreCase));
        }

        var result = query.Select(x => new BrgSearchResponse(
            x.BrgId,
            x.BrgName,
            x.KetBarang,
            x.GroupRekDkId,
            x.IsAktif
        ));

        return Task.FromResult(result);
    }
}
