using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListWorklistQuery(string BangsalId)
    : IRequest<IEnumerable<WaitingListWorklistView>>, IBangsalKey;

public class WaitingListWorklistHandler
    : IRequestHandler<WaitingListWorklistQuery, IEnumerable<WaitingListWorklistView>>
{
    private readonly IWaitingListWorklistDal _worklistDal;

    public WaitingListWorklistHandler(IWaitingListWorklistDal worklistDal) =>
        _worklistDal = worklistDal;

    public Task<IEnumerable<WaitingListWorklistView>> Handle(
        WaitingListWorklistQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BangsalId, nameof(request.BangsalId));

        var items = _worklistDal.ListActiveWorklist(request.BangsalId);
        return Task.FromResult(items);
    }
}
