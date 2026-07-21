using MediatR;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record PakaiBedIgdListOrphanQuery() : IRequest<IEnumerable<PakaiBedIgdOrphanView>>;

public class PakaiBedIgdListOrphanHandler : IRequestHandler<PakaiBedIgdListOrphanQuery, IEnumerable<PakaiBedIgdOrphanView>>
{
    private readonly IPakaiBedIgdRepo _pakaiRepo;

    public PakaiBedIgdListOrphanHandler(IPakaiBedIgdRepo pakaiRepo)
    {
        _pakaiRepo = pakaiRepo;
    }

    public Task<IEnumerable<PakaiBedIgdOrphanView>> Handle(PakaiBedIgdListOrphanQuery request, CancellationToken cancellationToken)
    {
        var result = _pakaiRepo.ListOrphans();
        return Task.FromResult(result);
    }
}
