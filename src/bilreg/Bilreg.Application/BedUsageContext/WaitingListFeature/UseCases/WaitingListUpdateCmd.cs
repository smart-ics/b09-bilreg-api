using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListUpdateCmd(
    string WaitingListId,
    string KelasId,
    string BangsalId,
    ClinicalUrgencyEnum ClinicalUrgency,
    string SpecialRequirements,
    int Priority,
    string UserId,
    string? PlannedBedId = null,
    string? PlannedBedName = null) : IRequest, IWaitingListKey;

public class WaitingListUpdateHandler : IRequestHandler<WaitingListUpdateCmd>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;
    private readonly IBedRepo _bedRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public WaitingListUpdateHandler(
        IWaitingListRepo waitingListRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo,
        IBedRepo bedRepo,
        ITglJamProvider tglJamProvider)
    {
        _waitingListRepo = waitingListRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
        _bedRepo = bedRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(WaitingListUpdateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId, nameof(request.WaitingListId));
        Guard.Against.NullOrWhiteSpace(request.KelasId, nameof(request.KelasId));
        Guard.Against.NullOrWhiteSpace(request.BangsalId, nameof(request.BangsalId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var waitingList = _waitingListRepo.LoadEntity(request)
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");

        var kelas = _kelasRepo.LoadEntity(KelasType.Key(request.KelasId))
            .GetValueOrThrow($"Kelas '{request.KelasId}' tidak ditemukan.")
            .ToReff();

        var bangsal = _bangsalRepo.LoadEntity(BangsalType.Key(request.BangsalId))
            .GetValueOrThrow($"Bangsal '{request.BangsalId}' tidak ditemukan.")
            .ToReff();

        var occurredAt = _tglJamProvider.Now;
        var updated = waitingList.Update(
            kelas,
            bangsal,
            request.ClinicalUrgency,
            request.SpecialRequirements ?? string.Empty,
            request.Priority,
            request.UserId,
            occurredAt);

        if (request.PlannedBedId is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.PlannedBedId) && request.PlannedBedId != "-")
            {
                var bed = _bedRepo.LoadEntity(BedType.Key(request.PlannedBedId))
                    .GetValueOrThrow($"Bed '{request.PlannedBedId}' tidak ditemukan.");
                updated = updated.ReservePlannedBed(bed.ToReff(), request.UserId, occurredAt);
            }
            else if (updated.WaitingListStatus == WaitingListStatusEnum.Reserved)
            {
                updated = updated.ClearPlannedBed(request.UserId, occurredAt);
            }
        }

        _waitingListRepo.Save(updated);

        return Task.CompletedTask;
    }
}
