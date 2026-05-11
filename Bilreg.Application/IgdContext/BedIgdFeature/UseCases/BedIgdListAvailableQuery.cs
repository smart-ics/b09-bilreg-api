using MediatR;

namespace Bilreg.Application.IgdContext.BedIgdFeature.UseCases;

public record BedIgdListAvailableQuery() : IRequest<IEnumerable<BedIgdView>>;

public class BedIgdListAvailableHandler : IRequestHandler<BedIgdListAvailableQuery, IEnumerable<BedIgdView>>
{
    private readonly IBedIgdRepo _repo;

    public BedIgdListAvailableHandler(IBedIgdRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<BedIgdView>> Handle(BedIgdListAvailableQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_repo.ListAvailable());
}
