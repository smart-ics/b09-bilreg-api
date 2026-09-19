using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using MediatR;

namespace Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases;

/// <summary>
/// Operational task rows of one IGD visit (architecture §5.2, §6.4, BR-30). Feeds
/// SCR-03 and the BILREG display; read-only, no delete/purge/archive surface (BR-31).
/// </summary>
public record IgdVisitListSmassTaskQuery(string IgdVisitId)
    : IRequest<IEnumerable<IgdVisitSmassTaskView>>;

/// <summary>
/// BILREG projection of one <see cref="IgdVisitSmassTaskModel"/> (architecture §5.2
/// §6.5, BR-30). <c>TaskType</c>/<c>TaskStatus</c> use the approved wire codes
/// (<c>GENERATE</c>/<c>LINK</c>, <c>PENDING</c>/<c>SUCCEEDED</c>/<c>FAILED</c>). The
/// sentinel <c>3000-01-01</c> dates are surfaced verbatim; conversion to <c>null</c> is a
/// client concern.
/// </summary>
public record IgdVisitSmassTaskView(
    string IgdVisitSmassTaskId,
    string IgdVisitId,
    int NoTriage,
    string TaskType,
    string TaskStatus,
    string AssessmentId,
    int RetryCount,
    DateTime LastRetryDate,
    DateTime ProcessedDate,
    string LastError,
    DateTime CrtDate)
{
    public static IgdVisitSmassTaskView FromModel(IgdVisitSmassTaskModel model)
        => new(
            model.IgdVisitSmassTaskId,
            model.IgdVisitId,
            model.NoTriage,
            model.TaskType.ToCode(),
            model.TaskStatus.ToCode(),
            model.AssessmentId,
            model.RetryCount,
            model.LastRetryDate,
            model.ProcessedDate,
            model.LastError,
            model.CrtDate);
}

public class IgdVisitListSmassTaskHandler
    : IRequestHandler<IgdVisitListSmassTaskQuery, IEnumerable<IgdVisitSmassTaskView>>
{
    private readonly IIgdVisitSmassTaskRepo _repo;

    public IgdVisitListSmassTaskHandler(IIgdVisitSmassTaskRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<IgdVisitSmassTaskView>> Handle(
        IgdVisitListSmassTaskQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));

        var list = _repo.ListByVisit(request.IgdVisitId)
            .Select(IgdVisitSmassTaskView.FromModel)
            .ToList();

        return Task.FromResult<IEnumerable<IgdVisitSmassTaskView>>(list);
    }
}
