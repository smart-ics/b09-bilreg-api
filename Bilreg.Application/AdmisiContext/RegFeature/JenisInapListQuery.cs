using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record JenisInapListQuery() : IRequest<IEnumerable<JenisInapListResponse>>;

public record JenisInapListResponse(string JenisInapId, string JenisInapName);

public class JenisInapListHandler : IRequestHandler<JenisInapListQuery, IEnumerable<JenisInapListResponse>>
{
    private readonly IJenisInapRepo _jenisInapRepo;

    public JenisInapListHandler(IJenisInapRepo jenisInapRepo)
    {
        _jenisInapRepo = jenisInapRepo;
    }

    public Task<IEnumerable<JenisInapListResponse>> Handle(JenisInapListQuery request, CancellationToken cancellationToken)
    {
        var listData = _jenisInapRepo.ListData() ?? [];
        var result = listData.Select(x =>
            new JenisInapListResponse(x.JenisInapId, x.JenisInapName));

        return Task.FromResult(result);
    }
}
