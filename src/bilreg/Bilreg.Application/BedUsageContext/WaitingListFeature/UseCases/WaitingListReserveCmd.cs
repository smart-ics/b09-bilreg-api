using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListReserveCmd(
    string WaitingListId,
    string BedId,
    string UserId,
    string? BedName = null) : IRequest, IWaitingListKey;

public class WaitingListReserveHandler : IRequestHandler<WaitingListReserveCmd>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IBedRepo _bedRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public WaitingListReserveHandler(
        IWaitingListRepo waitingListRepo,
        IBedRepo bedRepo,
        ITglJamProvider tglJamProvider)
    {
        _waitingListRepo = waitingListRepo;
        _bedRepo = bedRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(WaitingListReserveCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId, nameof(request.WaitingListId));
        Guard.Against.NullOrWhiteSpace(request.BedId, nameof(request.BedId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var waitingList = _waitingListRepo.LoadEntity(request)
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");

        var bed = _bedRepo.LoadEntity(BedType.Key(request.BedId))
            .GetValueOrThrow($"Bed '{request.BedId}' tidak ditemukan.");

        var occurredAt = _tglJamProvider.Now;
        var reserved = waitingList.ReservePlannedBed(bed.ToReff(), request.UserId, occurredAt);

        _waitingListRepo.Save(reserved);

        return Task.CompletedTask;
    }
}
