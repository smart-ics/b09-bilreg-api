using MediatR;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record BedIgdListQuery() : IRequest<IEnumerable<BedIgdView>>;

public class BedIgdListHandler : IRequestHandler<BedIgdListQuery, IEnumerable<BedIgdView>>
{
    private readonly IBedIgdRepo _repo;

    public BedIgdListHandler(IBedIgdRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<BedIgdView>> Handle(BedIgdListQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_repo.ListData());
}
