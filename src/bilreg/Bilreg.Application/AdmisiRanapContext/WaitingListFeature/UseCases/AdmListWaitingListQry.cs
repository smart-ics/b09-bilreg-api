using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;

public record AdmListWaitingListQry(
    string? BangsalId = null,
    int? WaitingListStatus = null) : IRequest<AdmListWaitingListResponse>;

public record AdmListWaitingListResponse(IReadOnlyList<WaitingListWorklistView> Items);

public class AdmListWaitingListHandler : IRequestHandler<AdmListWaitingListQry, AdmListWaitingListResponse>
{
    private readonly IWaitingListWorklistDal _worklistDal;

    public AdmListWaitingListHandler(IWaitingListWorklistDal worklistDal) =>
        _worklistDal = worklistDal;

    public Task<AdmListWaitingListResponse> Handle(
        AdmListWaitingListQry request,
        CancellationToken cancellationToken)
    {
        var items = _worklistDal
            .List(new WaitingListWorklistFilter(request.BangsalId, request.WaitingListStatus))
            .ToList();

        return Task.FromResult(new AdmListWaitingListResponse(items));
    }
}
