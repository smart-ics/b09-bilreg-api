using MediatR;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record PakaiBedListOrphanQuery() : IRequest<IEnumerable<PakaiBedOrphanView>>;

public class PakaiBedListOrphanHandler : IRequestHandler<PakaiBedListOrphanQuery, IEnumerable<PakaiBedOrphanView>>
{
    private readonly IPakaiBedRepo _pakaiRepo;

    public PakaiBedListOrphanHandler(IPakaiBedRepo pakaiRepo)
    {
        _pakaiRepo = pakaiRepo;
    }

    public Task<IEnumerable<PakaiBedOrphanView>> Handle(PakaiBedListOrphanQuery request, CancellationToken cancellationToken)
    {
        var result = _pakaiRepo.ListOrphans();
        return Task.FromResult(result);
    }
}
