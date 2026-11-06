using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListGetByRegQuery(string RegId)
    : IRequest<IEnumerable<WaitingListGetResponse>>, IRegKey;

public class WaitingListGetByRegHandler
    : IRequestHandler<WaitingListGetByRegQuery, IEnumerable<WaitingListGetResponse>>
{
    private readonly IWaitingListRepo _waitingListRepo;

    public WaitingListGetByRegHandler(IWaitingListRepo waitingListRepo) =>
        _waitingListRepo = waitingListRepo;

    public Task<IEnumerable<WaitingListGetResponse>> Handle(
        WaitingListGetByRegQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var models = _waitingListRepo.ListByRegId(request.RegId);
        var responses = models.Select(WaitingListGetResponse.FromModel).ToList();

        return Task.FromResult<IEnumerable<WaitingListGetResponse>>(responses);
    }
}
