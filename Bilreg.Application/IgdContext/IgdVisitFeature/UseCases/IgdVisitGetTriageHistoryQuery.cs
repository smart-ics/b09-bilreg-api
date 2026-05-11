using Ardalis.GuardClauses;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using MediatR;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitGetTriageHistoryQuery(string IgdVisitId) : IRequest<IEnumerable<TriageHistoryItem>>, IIgdVisitKey;

public record TriageHistoryItem(
    int NoTriage,
    string TriageMethod,
    string TriageLevel,
    string TriageColor,
    DateTime AssessmentDateTime,
    DateTime NextReTriageAt,
    string AssessorUserId,
    bool IsManualOverrideBlack,
    string OverrideReason,
    string Notes);

public class IgdVisitGetTriageHistoryHandler : IRequestHandler<IgdVisitGetTriageHistoryQuery, IEnumerable<TriageHistoryItem>>
{
    private readonly IIgdVisitRepo _repo;

    public IgdVisitGetTriageHistoryHandler(IIgdVisitRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<TriageHistoryItem>> Handle(IgdVisitGetTriageHistoryQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        var visit = _repo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        var items = visit.ListTriage
            .OrderByDescending(x => x.NoTriage)
            .Select(x => new TriageHistoryItem(
                x.NoTriage,
                x.Method.ToCode(),
                x.Level.ToCode(),
                x.Color.ToCode(),
                x.AssessmentDateTime,
                x.Level.ToReAssessmentInterval() is { } interval
                    ? x.AssessmentDateTime.Add(interval)
                    : new DateTime(3000, 1, 1),
                x.AssessorUserId,
                x.IsManualOverrideBlack,
                x.OverrideReason,
                x.Notes));
        return Task.FromResult(items);
    }
}
