using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgInTransitListQuery(string LayananTujuanId) 
    : IRequest<IEnumerable<MutasiBrgHeaderView>>, ILayananKey
{
    public string LayananId => LayananTujuanId;
}

public class MutasiBrgInTransitListHandler 
    : IRequestHandler<MutasiBrgInTransitListQuery, IEnumerable<MutasiBrgHeaderView>>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;

    public MutasiBrgInTransitListHandler(IMutasiBrgRepo mutasiBrgRepo)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
    }

    public Task<IEnumerable<MutasiBrgHeaderView>> Handle(MutasiBrgInTransitListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananTujuanId, nameof(request.LayananTujuanId));

        var list = _mutasiBrgRepo.ListInTransit(request.LayananTujuanId)?.ToList() ?? [];
        return Task.FromResult<IEnumerable<MutasiBrgHeaderView>>(list);
    }
}
