using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record JenisInapGetQuery(string JenisInapId) : IRequest<JenisInapGetResponse>, IJenisInapKey;

public record JenisInapGetResponse(string JenisInapId, string JenisInapName);

public class JenisInapGetHandler : IRequestHandler<JenisInapGetQuery, JenisInapGetResponse>
{
    private readonly IJenisInapRepo _jenisInapRepo;

    public JenisInapGetHandler(IJenisInapRepo jenisInapRepo)
    {
        _jenisInapRepo = jenisInapRepo;
    }

    public Task<JenisInapGetResponse> Handle(JenisInapGetQuery request, CancellationToken cancellationToken)
    {
        var jenisInap = _jenisInapRepo.LoadEntity(request)
            .GetValueOrThrow($"Jenis Inap {request.JenisInapId} not found");

        var result = new JenisInapGetResponse(
            jenisInap.JenisInapId,
            jenisInap.JenisInapName);

        return Task.FromResult(result);
    }
}