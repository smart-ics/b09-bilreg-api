using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListRerouteCmd(
    string WaitingListId,
    string TargetBangsalId,
    string Reason,
    string UserId,
    string? TargetKelasId = null,
    ClinicalUrgencyEnum? ClinicalUrgency = null,
    string? SpecialRequirements = null,
    string? PlannedBedId = null,
    string? PlannedBedName = null) : IRequest<WaitingListRerouteResponse>, IWaitingListKey;

public record WaitingListRerouteResponse(string PreviousWaitingListId, string NewWaitingListId);

public class WaitingListRerouteHandler : IRequestHandler<WaitingListRerouteCmd, WaitingListRerouteResponse>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IBangsalRepo _bangsalRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBedRepo _bedRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public WaitingListRerouteHandler(
        IWaitingListRepo waitingListRepo,
        IBangsalRepo bangsalRepo,
        IKelasRepo kelasRepo,
        IBedRepo bedRepo,
        ITglJamProvider tglJamProvider)
    {
        _waitingListRepo = waitingListRepo;
        _bangsalRepo = bangsalRepo;
        _kelasRepo = kelasRepo;
        _bedRepo = bedRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<WaitingListRerouteResponse> Handle(
        WaitingListRerouteCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId, nameof(request.WaitingListId));
        Guard.Against.NullOrWhiteSpace(request.TargetBangsalId, nameof(request.TargetBangsalId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var current = _waitingListRepo.LoadEntity(request)
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");

        var targetBangsal = _bangsalRepo.LoadEntity(BangsalType.Key(request.TargetBangsalId))
            .GetValueOrThrow($"Bangsal '{request.TargetBangsalId}' tidak ditemukan.")
            .ToReff();

        var targetKelas = !string.IsNullOrWhiteSpace(request.TargetKelasId)
            ? _kelasRepo.LoadEntity(KelasType.Key(request.TargetKelasId))
                .GetValueOrThrow($"Kelas '{request.TargetKelasId}' tidak ditemukan.")
                .ToReff()
            : current.KelasRawat;

        var occurredAt = _tglJamProvider.Now;
        var reroutedCurrent = current.Reroute(request.Reason, request.UserId, occurredAt);

        var urgency = request.ClinicalUrgency ?? current.ClinicalUrgency;
        var specialReq = request.SpecialRequirements ?? current.SpecialRequirements;

        var newEntry = WaitingListModel.Create(
            current.RegId,
            current.Pasien,
            targetKelas,
            targetBangsal,
            current.ArrivalSourceType,
            current.ReferringUnit,
            current.ReferringDoctor,
            urgency,
            specialReq,
            current.Priority,
            request.UserId,
            occurredAt,
            previousWaitListId: current.WaitingListId);

        if (!string.IsNullOrWhiteSpace(request.PlannedBedId) && request.PlannedBedId != "-")
        {
            var bed = _bedRepo.LoadEntity(BedType.Key(request.PlannedBedId))
                .GetValueOrThrow($"Bed '{request.PlannedBedId}' tidak ditemukan.");
            newEntry = newEntry.ReservePlannedBed(bed.ToReff(), request.UserId, occurredAt);
        }

        using var trans = TransHelper.NewScope();
        _waitingListRepo.Save(reroutedCurrent);
        _waitingListRepo.Save(newEntry);
        trans.Complete();

        return Task.FromResult(new WaitingListRerouteResponse(reroutedCurrent.WaitingListId, newEntry.WaitingListId));
    }
}
