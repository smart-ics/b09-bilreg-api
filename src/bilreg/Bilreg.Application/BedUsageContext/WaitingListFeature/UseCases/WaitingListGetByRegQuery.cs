using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListGetByRegQuery(string RegId)
    : IRequest<WaitingListGetResponse?>, IRegKey;

public class WaitingListGetByRegHandler
    : IRequestHandler<WaitingListGetByRegQuery, WaitingListGetResponse?>
{
    private readonly IWaitingListRepo _waitingListRepo;

    public WaitingListGetByRegHandler(IWaitingListRepo waitingListRepo) =>
        _waitingListRepo = waitingListRepo;

    public Task<WaitingListGetResponse?> Handle(
        WaitingListGetByRegQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var active = _waitingListRepo.LoadActiveByRegId(request.RegId);
        if (active.HasValue)
            return Task.FromResult<WaitingListGetResponse?>(WaitingListGetResponse.FromModel(active.Value));

        var latest = _waitingListRepo.ListByRegId(request.RegId).FirstOrDefault();
        if (latest is not null)
            return Task.FromResult<WaitingListGetResponse?>(WaitingListGetResponse.FromModel(latest));

        return Task.FromResult<WaitingListGetResponse?>(null);
    }
}
