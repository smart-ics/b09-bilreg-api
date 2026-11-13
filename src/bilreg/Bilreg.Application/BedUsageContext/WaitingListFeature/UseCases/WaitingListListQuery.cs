using MediatR;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListListQuery(
    string? BangsalId = null,
    int? WaitingListStatus = null) : IRequest<WaitingListListResponse>;

public record WaitingListListResponse(IReadOnlyList<WaitingListWorklistView> Items);

public class WaitingListListHandler : IRequestHandler<WaitingListListQuery, WaitingListListResponse>
{
    private readonly IWaitingListWorklistDal _worklistDal;

    public WaitingListListHandler(IWaitingListWorklistDal worklistDal) =>
        _worklistDal = worklistDal;

    public Task<WaitingListListResponse> Handle(
        WaitingListListQuery request,
        CancellationToken cancellationToken)
    {
        var items = _worklistDal
            .List(new WaitingListWorklistFilter(request.BangsalId, request.WaitingListStatus))
            .ToList();

        return Task.FromResult(new WaitingListListResponse(items));
    }
}
