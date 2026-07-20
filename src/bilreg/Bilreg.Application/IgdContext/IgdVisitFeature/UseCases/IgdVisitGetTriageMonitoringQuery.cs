using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitGetTriageMonitoringQuery : IRequest<IEnumerable<IgdVisitTriageMonitoringItem>>;

public record IgdVisitTriageMonitoringItem(
    string IgdVisitId,
    string VisitorName,
    string TriageLevel,
    string TriageColor,
    DateTime LastTriageAt,
    DateTime NextReTriageAt,
    bool IsOverdue);

public class IgdVisitGetTriageMonitoringHandler : IRequestHandler<IgdVisitGetTriageMonitoringQuery, IEnumerable<IgdVisitTriageMonitoringItem>>
{
    private readonly IIgdVisitRepo _repo;
    private readonly ITglJamProvider _tglJamProvider;

    public IgdVisitGetTriageMonitoringHandler(IIgdVisitRepo repo, ITglJamProvider tglJamProvider)
    {
        _repo = repo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<IEnumerable<IgdVisitTriageMonitoringItem>> Handle(IgdVisitGetTriageMonitoringQuery request, CancellationToken cancellationToken)
    {
        var now = _tglJamProvider.Now;
        var list = _repo.ListAktif()
            .Where(x => x.HasTriage)
            .Select(x => new IgdVisitTriageMonitoringItem(
                x.IgdVisitId,
                x.VisitorName,
                x.TriageLevel,
                x.TriageColor,
                x.LastTriageAt,
                x.NextReTriageAt,
                x.NextReTriageAt != new DateTime(3000, 1, 1) && x.NextReTriageAt < now))
            .OrderBy(x => x.NextReTriageAt)
            .ToList();
        return Task.FromResult<IEnumerable<IgdVisitTriageMonitoringItem>>(list);
    }
}
