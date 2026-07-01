using MediatR;

namespace Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;

public record JenisListQuery() : IRequest<IEnumerable<JenisListResponse>>;

public record JenisListResponse(string JenisId, string JenisName);

public class JenisListHandler : IRequestHandler<JenisListQuery, IEnumerable<JenisListResponse>>
{
    private readonly IJenisRepo _jenisRepo;

    public JenisListHandler(IJenisRepo jenisRepo)
    {
        _jenisRepo = jenisRepo;
    }

    public Task<IEnumerable<JenisListResponse>> Handle(JenisListQuery request, CancellationToken cancellationToken)
    {
        var listJenis= _jenisRepo.ListData()?.ToList() ?? [];
        var response = listJenis
            .OrderBy(x => x.JenisName)
            .Select(x => new JenisListResponse(x.JenisId, x.JenisName));

        return Task.FromResult(response);
    }
}