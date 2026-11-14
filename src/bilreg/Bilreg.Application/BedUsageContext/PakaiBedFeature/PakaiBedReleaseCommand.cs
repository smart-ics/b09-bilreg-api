using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.Integration;
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
    string? CheckoutReason = null,
    string? DischargeScope = null,
    string? BabyRegId = null
) : IRequest<PakaiBedReleaseResult>;

public record PakaiBedReleaseResult(
    string PakaiBedId,
    DateTime TglJamOut,
    RoomChargeReconcileResponse? ReconcileResult = null,
    string? BangsalId = null,
    string? BedId = null,
    bool IsBatalCheckIn = false,
    string? RevertedWaitingListBangsalId = null,
    bool IsCleaningTriggered = true
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
    private readonly IEmr20AntrianRanapGateway? _emr20Gateway;

    public PakaiBedReleaseCommandHandler(
        IPakaiBedRepo pakaiBedRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider,
        IWaitingListRepo? waitingListRepo = null,
        IBedRepo? bedRepo = null,
        IKamarRepo? kamarRepo = null,
        ITindakanRepo? tindakanRepo = null,
        IBedOperationalRepo? bedOperationalRepo = null,
        IEmr20AntrianRanapGateway? emr20Gateway = null)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
        _waitingListRepo = waitingListRepo;
        _bedRepo = bedRepo;
        _kamarRepo = kamarRepo;
        _tindakanRepo = tindakanRepo;
        _bedOperationalRepo = bedOperationalRepo;
        _emr20Gateway = emr20Gateway;
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

        var isBatal = !string.IsNullOrWhiteSpace(request.CheckoutReason) &&
                      request.CheckoutReason.Contains("batal", StringComparison.OrdinalIgnoreCase);

        var isBayiScope = string.Equals(request.DischargeScope, "bayi", StringComparison.OrdinalIgnoreCase);

        string? bangsalId = pakaiBed.Layanan?.LayananId;
        string? kamarId = null;
        var bedId = (pakaiBed.Bed?.BedId ?? string.Empty).Trim();
        if (_bedRepo != null && !string.IsNullOrWhiteSpace(bedId))
        {
            var maybeBed = _bedRepo.LoadEntity(BedType.Key(bedId));
            if (maybeBed.HasValue)
            {
                bangsalId = !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-"
                    ? maybeBed.Value.Bangsal.BangsalId
                    : bangsalId;
                kamarId = maybeBed.Value.Kamar?.KamarId;
            }
        }
        if ((string.IsNullOrWhiteSpace(bangsalId) || bangsalId == "-") && _kamarRepo != null && !string.IsNullOrWhiteSpace(kamarId))
        {
            var maybeKamar = _kamarRepo.LoadEntity(KamarType.Key(kamarId));
            if (maybeKamar.HasValue && !string.IsNullOrWhiteSpace(maybeKamar.Value.Bangsal?.BangsalId) && maybeKamar.Value.Bangsal.BangsalId != "-")
            {
                bangsalId = maybeKamar.Value.Bangsal.BangsalId;
            }
        }

        // --- BRANCH 1: BAYI SCOPE (Isolated Child Discharge) ---
        if (isBayiScope)
        {
            PakaiBedModel? babyPakaiBed = null;

            if (pakaiBed.Tarif == 0 || (!string.IsNullOrWhiteSpace(request.BabyRegId) && pakaiBed.Reg.RegId == request.BabyRegId))
            {
                babyPakaiBed = pakaiBed;
            }
            else
            {
                // Action invoked from mother's bed row; locate rooming-in baby record
                if (!string.IsNullOrWhiteSpace(request.BabyRegId))
                {
                    var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(request.BabyRegId))?.ToList() ?? [];
                    babyPakaiBed = babyRecords.FirstOrDefault(x =>
                        (string.IsNullOrWhiteSpace(bedId) || x.Bed.BedId == bedId) &&
                        (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                }

                if (babyPakaiBed == null && !string.IsNullOrWhiteSpace(bedId))
                {
                    var babyTrsIds = await _pakaiBedRepo.GetRoomingInBabyTrsIdsAsync(bedId, request.BabyRegId);
                    if (babyTrsIds != null && babyTrsIds.Count > 0)
                    {
                        var loaded = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(babyTrsIds[0]));
                        if (loaded.HasValue)
                        {
                            babyPakaiBed = loaded.Value;
                        }
                    }
                }

                if (babyPakaiBed == null)
                {
                    var babyRegIds = await _pakaiBedRepo.GetRoomingInBabyRegIdsAsync(pakaiBed.PakaiBedId);
                    if (babyRegIds != null)
                    {
                        foreach (var bReg in babyRegIds)
                        {
                            var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(bReg))?.ToList() ?? [];
                            babyPakaiBed = babyRecords.FirstOrDefault(x =>
                                (string.IsNullOrWhiteSpace(bedId) || x.Bed.BedId == bedId) &&
                                (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                            if (babyPakaiBed != null) break;
                        }
                    }
                }
            }

            var checkInTimestamp = babyPakaiBed?.Periode.Masuk.Timestamp ?? pakaiBed.Periode.Masuk.Timestamp;
            if (checkInTimestamp != default && occurredAt < checkInTimestamp)
            {
                if (occurredAt.Date == checkInTimestamp.Date)
                {
                    occurredAt = checkInTimestamp;
                }
                else
                {
                    throw new InvalidOperationException("Waktu keluar tidak boleh lebih awal dari waktu masuk.");
                }
            }

            if (isBatal && _tindakanRepo != null)
            {
                var checkRegId = babyPakaiBed?.Reg.RegId ?? request.BabyRegId;
                if (!string.IsNullOrWhiteSpace(checkRegId))
                {
                    var targetPkbId = babyPakaiBed?.PakaiBedId ?? request.PakaiBedId;
                    var activeTindakans = _tindakanRepo.ListDataTdkJual(RegModel.Key(checkRegId))
                        ?.Where(t => t.PakaiBedId == targetPkbId || string.IsNullOrWhiteSpace(t.PakaiBedId))
                        ?.ToList() ?? [];

                    if (activeTindakans.Count > 0)
                    {
                        throw new InvalidOperationException("Batal pemakaian bed tidak diizinkan karena pasien masih memiliki tindakan aktif.");
                    }
                }
            }

            if (babyPakaiBed != null)
            {
                if (isBatal)
                {
                    babyPakaiBed.Void(request.UserId, occurredAt);
                }
                babyPakaiBed.Release(request.UserId, occurredAt);
                _pakaiBedRepo.SaveChanges(babyPakaiBed);
            }

            if (!string.IsNullOrWhiteSpace(bedId))
            {
                await _pakaiBedRepo.ReleaseRoomingInBabyAsync(bedId, request.BabyRegId ?? babyPakaiBed?.Reg.RegId, request.UserId, occurredAt);
            }

            var babyDischargeRegId = babyPakaiBed?.Reg.RegId ?? request.BabyRegId;
            if (_emr20Gateway != null && !string.IsNullOrWhiteSpace(babyDischargeRegId))
            {
                await _emr20Gateway.DischargeRegRanapAsync(babyDischargeRegId, isGabung: true, cancellationToken);
            }

            // Bayi discharge does NOT trigger room charge reconcile and does NOT mark bed as CleaningRequired
            return new PakaiBedReleaseResult(
                babyPakaiBed?.PakaiBedId ?? request.PakaiBedId,
                occurredAt,
                ReconcileResult: null,
                BangsalId: bangsalId,
                BedId: bedId,
                IsBatalCheckIn: isBatal,
                RevertedWaitingListBangsalId: null,
                IsCleaningTriggered: false
            );
        }

        // --- BRANCH 2: IBU / DEFAULT SCOPE (Cascade Discharge) ---
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
        }

        // Cascade release associated rooming-in baby on the same bed (DISCOVER BEFORE MODIFYING MOTHER RECORD)
        var babyRegsToDischarge = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(request.BabyRegId))
        {
            babyRegsToDischarge.Add(request.BabyRegId.Trim());
        }

        var detectedBabyRegs = await _pakaiBedRepo.GetRoomingInBabyRegIdsAsync(pakaiBed.PakaiBedId);
        if (detectedBabyRegs != null)
        {
            foreach (var br in detectedBabyRegs.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                babyRegsToDischarge.Add(br.Trim());
            }
        }

        var activeBabyBedsToDischarge = new List<PakaiBedModel>();
        foreach (var babyReg in babyRegsToDischarge)
        {
            var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(babyReg))?.ToList() ?? [];
            var activeBabyBed = babyRecords.FirstOrDefault(x =>
                (string.IsNullOrWhiteSpace(bedId) || x.Bed.BedId == bedId) &&
                (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
            if (activeBabyBed != null)
            {
                activeBabyBedsToDischarge.Add(activeBabyBed);
            }
        }

        if (isBatal)
        {
            pakaiBed.Void(request.UserId, occurredAt);
        }
        pakaiBed.Release(request.UserId, occurredAt);
        _pakaiBedRepo.SaveChanges(pakaiBed);

        foreach (var activeBabyBed in activeBabyBedsToDischarge)
        {
            if (isBatal)
            {
                activeBabyBed.Void(request.UserId, occurredAt);
            }
            activeBabyBed.Release(request.UserId, occurredAt);
            _pakaiBedRepo.SaveChanges(activeBabyBed);
        }

        if (!string.IsNullOrWhiteSpace(bedId))
        {
            await _pakaiBedRepo.ReleaseRoomingInBabyAsync(bedId, null, request.UserId, occurredAt);
        }

        if (_emr20Gateway != null)
        {
            await _emr20Gateway.DischargeRegRanapAsync(pakaiBed.Reg.RegId, isGabung: false, cancellationToken);
            foreach (var babyReg in babyRegsToDischarge)
            {
                if (!string.Equals(babyReg, pakaiBed.Reg.RegId, StringComparison.OrdinalIgnoreCase))
                {
                    await _emr20Gateway.DischargeRegRanapAsync(babyReg, isGabung: true, cancellationToken);
                }
            }
        }

        // Checkout hook: trigger/send RoomChargeReconcileCheckoutCmd via _mediator only for normal discharge
        RoomChargeReconcileResponse? reconcileResult = null;
        if (!isBatal)
        {
            var reconcileCmd = new RoomChargeReconcileCheckoutCmd(request.PakaiBedId, occurredAt, request.UserId);
            reconcileResult = await _mediator.Send(reconcileCmd, cancellationToken);
        }

        // Update BedOperational: mark bed as CleaningRequired (HK Dirty) on mother release
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
                        !string.IsNullOrWhiteSpace(bangsalId) && bangsalId != "-" ? bangsalId : "DEFAULT",
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

        return new PakaiBedReleaseResult(request.PakaiBedId, occurredAt, reconcileResult, bangsalId, bedId, isBatal, revertedWlBangsalId, IsCleaningTriggered: true);
    }
}
