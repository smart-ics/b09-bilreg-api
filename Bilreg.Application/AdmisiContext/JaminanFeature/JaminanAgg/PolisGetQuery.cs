using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public record PolisGetQuery(string PolisId) : IRequest<PolisModel>, IPolisKey;

public class PolisGetHandler : IRequestHandler<PolisGetQuery, PolisModel>
{
    private readonly IPolisRepo _polisRepo;

    public PolisGetHandler(IPolisRepo polisRepo)
    {
        _polisRepo = polisRepo;
    }

    public Task<PolisModel> Handle(PolisGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PolisId, nameof(request.PolisId));

        var polis = _polisRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Polis {request.PolisId} not found")
                );

        return Task.FromResult(polis);
    }
}
