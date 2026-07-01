using MediatR;

namespace Farinv.Application.BrgContext.BrgFeature.UseCases;

public record SatuanListQuery() : IRequest<IEnumerable<SatuanListResponse>>;

public record SatuanListResponse(string SatuanId, string SatuanName);

public class SatuanListHandler : IRequestHandler<SatuanListQuery, IEnumerable<SatuanListResponse>>
{
    private readonly ISatuanRepo _satuanRepo;

    public SatuanListHandler(ISatuanRepo satuanRepo)
    {
        _satuanRepo = satuanRepo;
    }

    public Task<IEnumerable<SatuanListResponse>> Handle(SatuanListQuery request, CancellationToken cancellationToken)
    {
        var listSatuan = _satuanRepo.ListData()?.ToList() ?? [];
        var response = listSatuan
            .OrderBy(x => x.SatuanName)
            .Select(x => new SatuanListResponse(x.SatuanId, x.SatuanName));

        return Task.FromResult(response);
    }
}