using MediatR;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitListAktifQuery() : IRequest<IEnumerable<IgdVisitView>>;

public class IgdVisitListAktifHandler : IRequestHandler<IgdVisitListAktifQuery, IEnumerable<IgdVisitView>>
{
    private readonly IIgdVisitRepo _repo;

    public IgdVisitListAktifHandler(IIgdVisitRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<IgdVisitView>> Handle(IgdVisitListAktifQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_repo.ListAktif());
}
