using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public record PakaiBedReleaseCommand(
    string PakaiBedId,
    DateTime TglJamOut,
    string UserId,
    string? CheckoutReason = null
) : IRequest<PakaiBedReleaseResult>;

public record PakaiBedReleaseResult(
    string PakaiBedId,
    DateTime TglJamOut,
    RoomChargeReconcileResponse? ReconcileResult = null,
    string? BangsalId = null,
    string? BedId = null,
    bool IsBatalCheckIn = false,
    string? RevertedWaitingListBangsalId = null
);

public class PakaiBedReleaseCommandHandler : IRequestHandler<PakaiBedReleaseCommand, PakaiBedReleaseResult>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IBedOperationalRepo? _bedOperationalRepo;
    private readonly IWaitingListRepo? _waitingListRepo;
    private readonly IBedRepo? _bedRepo;
    private readonly IKamarRepo? _kamarRepo;
    private readonly ITindakanRepo? _tindakanRepo;

    public PakaiBedReleaseCommandHandler(
        IPakaiBedRepo pakaiBedRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider,
        IWaitingListRepo? waitingListRepo = null,
        IBedRepo? bedRepo = null,
        IKamarRepo? kamarRepo = null,
        ITindakanRepo? tindakanRepo = null,
        IBedOperationalRepo? bedOperationalRepo = null)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
        _waitingListRepo = waitingListRepo;
        _bedRepo = bedRepo;
        _kamarRepo = kamarRepo;
        _tindakanRepo = tindakanRepo;
        _bedOperationalRepo = bedOperationalRepo;
    }

    public async Task<PakaiBedReleaseResult> Handle(PakaiBedReleaseCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        var pakaiBed = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId))
            .GetValueOrThrow($"Pakai Bed '{request.PakaiBedId}' not found");

        var occurredAt = SimulationTimeGuard.Resolve(
            request.TglJamOut,
            _tglJamProvider,
            operationName: "PakaiBedRelease");

        // Ensure occurredAt is not earlier than check-in time
        if (pakaiBed.Periode.Masuk.Timestamp != default && occurredAt < pakaiBed.Periode.Masuk.Timestamp)
        {
            if (occurredAt.Date == pakaiBed.Periode.Masuk.Timestamp.Date)
            {
                occurredAt = pakaiBed.Periode.Masuk.Timestamp;
            }
            else
            {
                throw new InvalidOperationException("Waktu keluar tidak boleh lebih awal dari waktu masuk.");
            }
        }

        var isBatal = !string.IsNullOrWhiteSpace(request.CheckoutReason) &&
                      request.CheckoutReason.Contains("batal", StringComparison.OrdinalIgnoreCase);

        if (isBatal)
        {
            if (_tindakanRepo != null)
            {
                var activeTindakans = _tindakanRepo.ListDataTdkJual(RegModel.Key(pakaiBed.Reg.RegId))
                    ?.Where(t => t.PakaiBedId == request.PakaiBedId || string.IsNullOrWhiteSpace(t.PakaiBedId))
                    ?.ToList() ?? [];

                if (activeTindakans.Count > 0)
                {
                    throw new InvalidOperationException("Batal pemakaian bed tidak diizinkan karena pasien masih memiliki tindakan aktif.");
                }
            }

            pakaiBed.Void(request.UserId, occurredAt);
        }
        pakaiBed.Release(request.UserId, occurredAt);
        _pakaiBedRepo.SaveChanges(pakaiBed);

        // Checkout hook: trigger/send RoomChargeReconcileCheckoutCmd via _mediator only for normal discharge
        RoomChargeReconcileResponse? reconcileResult = null;
        if (!isBatal)
        {
            var reconcileCmd = new RoomChargeReconcileCheckoutCmd(request.PakaiBedId, occurredAt, request.UserId);
            reconcileResult = await _mediator.Send(reconcileCmd, cancellationToken);
        }

        string? bangsalId = pakaiBed.Layanan?.LayananId;
        string? kamarId = null;
        var bedId = pakaiBed.Bed?.BedId;
        if (_bedRepo != null && !string.IsNullOrWhiteSpace(bedId))
        {
            var maybeBed = _bedRepo.LoadEntity(BedType.Key(bedId));
            if (maybeBed.HasValue)
            {
                bangsalId = maybeBed.Value.Bangsal?.BangsalId ?? bangsalId;
                kamarId = maybeBed.Value.Kamar?.KamarId;
            }
        }
        if (string.IsNullOrWhiteSpace(bangsalId) && _kamarRepo != null && !string.IsNullOrWhiteSpace(kamarId))
        {
            var maybeKamar = _kamarRepo.LoadEntity(KamarType.Key(kamarId));
            if (maybeKamar.HasValue)
            {
                bangsalId = maybeKamar.Value.Bangsal?.BangsalId;
            }
        }

        // 5b. Update BedOperational: always mark bed as CleaningRequired (HK Dirty) on all releases (including cancellation)
        if (_bedOperationalRepo != null && !string.IsNullOrWhiteSpace(bedId))
        {
            var nowUtc = DateTime.UtcNow;
            var occurredAtUtc = occurredAt.Kind == DateTimeKind.Utc ? occurredAt : occurredAt.ToUniversalTime();
            if (nowUtc < occurredAtUtc)
            {
                nowUtc = occurredAtUtc;
            }

            var bedOperational = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(bedId))
                .Match(
                    onSome: bo => bo,
                    onNone: () => BedOperationalModel.Create(
                        bedId,
                        !string.IsNullOrWhiteSpace(bangsalId) ? bangsalId : "DEFAULT",
                        !string.IsNullOrWhiteSpace(kamarId) ? kamarId : "DEFAULT",
                        OccupancyPolicyReff.Default)
                );

            var defaultReason = isBatal
                ? $"Pembersihan Pasca Batal Check-In: {pakaiBed.Reg.RegId}"
                : $"Pasien pulang: {pakaiBed.Reg.RegId}";

            var dirtyReason = isBatal
                ? (!string.IsNullOrWhiteSpace(request.CheckoutReason)
                    ? (request.CheckoutReason.StartsWith("Pembersihan Pasca Batal Check-In", StringComparison.OrdinalIgnoreCase)
                        ? request.CheckoutReason
                        : $"Pembersihan Pasca Batal Check-In: {request.CheckoutReason}")
                    : defaultReason)
                : (string.IsNullOrWhiteSpace(request.CheckoutReason) ? defaultReason : request.CheckoutReason);

            var factPrefix = isBatal ? "BATAL" : "DISCHARGE";
            var factId = $"{factPrefix}-{pakaiBed.PakaiBedId}";
            if (factId.Length > 50)
            {
                factId = factId[..50];
            }

            bedOperational = bedOperational.SetCleaningRequired(
                dirtyReason,
                request.UserId,
                occurredAtUtc,
                nowUtc,
                sourceFactId: factId,
                requestId: $"REQ-{Guid.NewGuid():N}");

            _bedOperationalRepo.SaveChanges(bedOperational);
        }

        string? revertedWlBangsalId = null;
        if (isBatal && _waitingListRepo != null)
        {
            var regId = pakaiBed.Reg.RegId;
            var waitingLists = _waitingListRepo.ListByRegId(regId)?.ToList() ?? [];
            var matchingWl = waitingLists.FirstOrDefault(w =>
                (!string.IsNullOrWhiteSpace(request.PakaiBedId) && w.PlacedPakaiBedId == request.PakaiBedId) ||
                (!string.IsNullOrWhiteSpace(bedId) && w.PlacedBedId == bedId) ||
                w.WaitingListStatus == WaitingListStatusEnum.Placed)
                ?? (!string.IsNullOrWhiteSpace(bangsalId) ? waitingLists.FirstOrDefault(w => w.Bangsal?.BangsalId == bangsalId) : null)
                ?? waitingLists.FirstOrDefault();

            if (matchingWl != null)
            {
                if (string.IsNullOrWhiteSpace(bangsalId))
                {
                    bangsalId = matchingWl.Bangsal?.BangsalId;
                }
                revertedWlBangsalId = matchingWl.Bangsal?.BangsalId;
                var revertedWl = matchingWl.RevertToWaiting(request.UserId, occurredAt);
                _waitingListRepo.SaveChanges(revertedWl);
            }
        }

        return new PakaiBedReleaseResult(request.PakaiBedId, occurredAt, reconcileResult, bangsalId, bedId, isBatal, revertedWlBangsalId);
    }
}
