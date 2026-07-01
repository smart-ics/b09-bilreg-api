using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public record PolisListQuery(string PasienId) : IRequest<IEnumerable<PolisView>>, IPasienKey;

public class PolisListHandler : IRequestHandler<PolisListQuery, IEnumerable<PolisView>>
{
    private readonly IPolisRepo _polisRepo;

    public PolisListHandler(IPolisRepo polisRepo)
    {
        _polisRepo = polisRepo;
    }

    public Task<IEnumerable<PolisView>> Handle(PolisListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId, nameof(request.PasienId));

        var listPolis = _polisRepo.ListData(request)?.ToList() ?? [];

        return Task.FromResult(listPolis.AsEnumerable());
    }
}
