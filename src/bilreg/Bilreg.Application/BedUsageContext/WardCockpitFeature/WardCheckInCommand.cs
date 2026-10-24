using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WardCockpitFeature;

public record WardCheckInCommand(
    string WaitingListId,
    string BedId,
    string? TipeKamarId,
    string UserId,
    DateTime? TglJamIn = null,
    string? LayananId = null,
    string? RegId = null
) : IRequest<WardCheckInResult>;

public record WardCheckInResult(
    string PakaiBedId,
    string WaitingListId,
    string BedId,
    string RegId,
    string Status
);

public class WardCheckInCommandHandler : IRequestHandler<WardCheckInCommand, WardCheckInResult>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IBedOperationalRepo _bedOperationalRepo;
    private readonly IBedRepo _bedRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IBangsalRepo _bangsalRepo;
    private readonly IRoomRateRepo _roomRateRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public WardCheckInCommandHandler(
        IWaitingListRepo waitingListRepo,
        IBedOperationalRepo bedOperationalRepo,
        IBedRepo bedRepo,
        IKamarRepo kamarRepo,
        IBangsalRepo bangsalRepo,
        IRoomRateRepo roomRateRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _waitingListRepo = waitingListRepo;
        _bedOperationalRepo = bedOperationalRepo;
        _bedRepo = bedRepo;
        _kamarRepo = kamarRepo;
        _bangsalRepo = bangsalRepo;
        _roomRateRepo = roomRateRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<WardCheckInResult> Handle(
        WardCheckInCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId, nameof(request.WaitingListId));
        Guard.Against.NullOrWhiteSpace(request.BedId, nameof(request.BedId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Validasi WaitingList
        var waitingList = _waitingListRepo.LoadEntity(WaitingListModel.Key(request.WaitingListId))
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");

        if (!waitingList.IsActive)
            throw new InvalidOperationException(
                $"Waiting List '{request.WaitingListId}' tidak aktif (status saat ini: {waitingList.WaitingListStatus}).");

        // 2. Validasi Kesiapan Operasional Bed (HK / Maintenance Blocker)
        var maybeBedOp = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(request.BedId));
        if (maybeBedOp.HasValue)
        {
            var bedOp = maybeBedOp.Value;
            if (bedOp.CurrentReadiness.HasValue && bedOp.CurrentReadiness.Value != BedReadinessStatusEnum.Ready)
            {
                throw new InvalidOperationException(
                    $"Bed '{request.BedId}' tidak siap pakai (status kesiapan: {bedOp.CurrentReadiness.Value}).");
            }

            if (bedOp.HasUnresolvedBlocker)
            {
                throw new InvalidOperationException(
                    $"Bed '{request.BedId}' memiliki kendala/blocker aktif: {bedOp.CurrentBlockerState.Reason}.");
            }
        }

        // 3. Menentukan parameter alokasi
        var regId = !string.IsNullOrWhiteSpace(request.RegId)
            ? request.RegId
            : waitingList.RegId;

        var tglJamIn = request.TglJamIn ?? _tglJamProvider.Now;

        var layananId = request.LayananId;
        if (string.IsNullOrWhiteSpace(layananId))
        {
            if (!string.IsNullOrWhiteSpace(waitingList.ReferringUnit.LayananId) &&
                waitingList.ReferringUnit.LayananId != "-")
            {
                layananId = waitingList.ReferringUnit.LayananId;
            }
            else
            {
                var bed = _bedRepo.LoadEntity(BedType.Key(request.BedId))
                    .GetValueOrThrow($"Bed '{request.BedId}' tidak ditemukan.");
                var bangsal = _bangsalRepo.LoadEntity(BangsalType.Key(bed.Bangsal.BangsalId))
                    .GetValueOrThrow($"Bangsal '{bed.Bangsal.BangsalId}' tidak ditemukan.");
                layananId = bangsal.Layanan.LayananId;
            }
        }

        var tipeKamarId = request.TipeKamarId;
        if (string.IsNullOrWhiteSpace(tipeKamarId))
        {
            var bed = _bedRepo.LoadEntity(BedType.Key(request.BedId))
                .GetValueOrThrow($"Bed '{request.BedId}' tidak ditemukan.");
            var kamar = _kamarRepo.LoadEntity(KamarType.Key(bed.Kamar.KamarId))
                .GetValueOrThrow($"Kamar '{bed.Kamar.KamarId}' tidak ditemukan.");
            var roomRate = _roomRateRepo.LoadEntity(kamar)
                .GetValueOrThrow($"Room Rate untuk Kamar '{kamar.KamarId}' tidak ditemukan.");
            var firstTipe = roomRate.ListTipe.FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Tidak ada Tipe Kamar pada Room Rate untuk Kamar '{kamar.KamarId}'.");
            tipeKamarId = firstTipe.TipeKamar.TipeKamarId;
        }

        // 4. Eksekusi alokasi via PakaiBedCreateCommand
        var createCmd = new PakaiBedCreateCommand(
            regId,
            layananId,
            tipeKamarId,
            request.BedId,
            request.UserId,
            tglJamIn,
            request.WaitingListId
        );

        var createResult = await _mediator.Send(createCmd, cancellationToken);

        return new WardCheckInResult(
            createResult.PakaiBedId,
            request.WaitingListId,
            request.BedId,
            regId,
            "Placed"
        );
    }
}
