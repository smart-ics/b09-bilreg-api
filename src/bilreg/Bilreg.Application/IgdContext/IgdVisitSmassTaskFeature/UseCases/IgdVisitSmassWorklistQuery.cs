using MediatR;

namespace Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases;

/// <summary>
/// Failed SMASS tasks across visits, oldest first (architecture §5.2, §9.4, BR-12,
/// BR-33). Operator monitoring is endpoint-only (AR-06) and read-only: no delete,
/// purge or archive path exists (BR-31).
/// </summary>
public record IgdVisitSmassWorklistQuery : IRequest<IEnumerable<IgdVisitSmassTaskView>>;

public class IgdVisitSmassWorklistHandler
    : IRequestHandler<IgdVisitSmassWorklistQuery, IEnumerable<IgdVisitSmassTaskView>>
{
    private readonly IIgdVisitSmassTaskRepo _repo;

    public IgdVisitSmassWorklistHandler(IIgdVisitSmassTaskRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<IgdVisitSmassTaskView>> Handle(
        IgdVisitSmassWorklistQuery request,
        CancellationToken cancellationToken)
    {
        // ListProcessable() returns Failed tasks ordered by CrtDate ascending (BR-12/BR-33).
        var list = _repo.ListProcessable()
            .Select(IgdVisitSmassTaskView.FromModel)
            .ToList();

        return Task.FromResult<IEnumerable<IgdVisitSmassTaskView>>(list);
    }
}
