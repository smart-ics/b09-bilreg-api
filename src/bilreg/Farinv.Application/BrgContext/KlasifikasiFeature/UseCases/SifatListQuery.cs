using MediatR;

namespace Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;

public record SifatListQuery() : IRequest<IEnumerable<SifatListResponse>>;

public record SifatListResponse(string SifatId, string SifatName);

public class SifatListHandler : IRequestHandler<SifatListQuery, IEnumerable<SifatListResponse>>
{
    private readonly ISifatRepo _sifatRepo;

    public SifatListHandler(ISifatRepo sifatRepo)
    {
        _sifatRepo = sifatRepo;
    }

    public Task<IEnumerable<SifatListResponse>> Handle(SifatListQuery request, CancellationToken cancellationToken)
    {
        var listSifat = _sifatRepo.ListData()?.ToList() ?? [];
        var response = listSifat
            .OrderBy(x => x.SifatName)
            .Select(x => new SifatListResponse(x.SifatId, x.SifatName));

        return Task.FromResult(response);
    }
}