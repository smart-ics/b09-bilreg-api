using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListRejectCmd(
    string WaitingListId,
    string Reason,
    string UserId) : IRequest, IWaitingListKey;

public class WaitingListRejectHandler : IRequestHandler<WaitingListRejectCmd>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public WaitingListRejectHandler(
        IWaitingListRepo waitingListRepo,
        ITglJamProvider tglJamProvider)
    {
        _waitingListRepo = waitingListRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(WaitingListRejectCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId, nameof(request.WaitingListId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var waitingList = _waitingListRepo.LoadEntity(request)
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");

        var occurredAt = _tglJamProvider.Now;
        var rejected = waitingList.Reject(request.Reason, request.UserId, occurredAt);

        _waitingListRepo.Save(rejected);

        return Task.CompletedTask;
    }
}
