using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using System.Text.Json.Serialization;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;

public record PakaiBedTransferCommand : IRequest<PakaiBedTransferResult>
{
    public string PakaiBedId { get; init; } = string.Empty;
    public string TargetBedId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime TglJamTransfer { get; init; }
    public string? AlasanPindah { get; init; }
    public string? TargetTipeKamarId { get; init; }
    public string? TargetLayananId { get; init; }
    public string? TransferScope { get; init; }
    public string? BabyRegId { get; init; }

    public string PakaiBedLamaId => PakaiBedId;

    [JsonConstructor]
    public PakaiBedTransferCommand(
        string PakaiBedId,
        string TargetBedId,
        string UserId,
        DateTime TglJamTransfer = default,
        string? AlasanPindah = null,
        string? TargetTipeKamarId = null,
        string? TargetLayananId = null,
        string? TransferScope = null,
        string? BabyRegId = null)
    {
        this.PakaiBedId = PakaiBedId;
        this.TargetBedId = TargetBedId;
        this.UserId = UserId;
        this.TglJamTransfer = TglJamTransfer;
        this.AlasanPindah = AlasanPindah;
        this.TargetTipeKamarId = TargetTipeKamarId;
        this.TargetLayananId = TargetLayananId;
        this.TransferScope = TransferScope;
        this.BabyRegId = BabyRegId;
    }

    public PakaiBedTransferCommand(
        string PakaiBedId,
        string TargetBedId,
        string UserId) 
        : this(PakaiBedId, TargetBedId, UserId, default, null, null, null, null, null)
    {
    }
}

public record PakaiBedTransferResult(
    string PakaiBedBaruId,
    string PakaiBedLamaId,
    string TargetBedId,
    DateTime TglJamTransfer,
    string SourceBedId = "",
    string SourceBangsalId = "",
    string TargetBangsalId = "",
    bool IsCleaningTriggered = true,
    string? BabyPakaiBedBaruId = null
);

public class PakaiBedTransferCommandHandler : IRequestHandler<PakaiBedTransferCommand, PakaiBedTransferResult>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IBedRepo _bedRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly ITipeKamarRepo _tipeKamarRepo;
    private readonly IRoomRateRepo _roomRateRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IRegRepo _regRepo;
    private readonly IBedOperationalRepo _bedOperationalRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public PakaiBedTransferCommandHandler(
        IPakaiBedRepo pakaiBedRepo,
        IBedRepo bedRepo,
        IKamarRepo kamarRepo,
        IKelasRepo kelasRepo,
        ITipeKamarRepo tipeKamarRepo,
        IRoomRateRepo roomRateRepo,
        ILayananRepo layananRepo,
        IRegRepo regRepo,
        IBedOperationalRepo bedOperationalRepo,
        ITglJamProvider tglJamProvider)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _bedRepo = bedRepo;
        _kamarRepo = kamarRepo;
        _kelasRepo = kelasRepo;
        _tipeKamarRepo = tipeKamarRepo;
        _roomRateRepo = roomRateRepo;
        _layananRepo = layananRepo;
        _regRepo = regRepo;
        _bedOperationalRepo = bedOperationalRepo;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<PakaiBedTransferResult> Handle(PakaiBedTransferCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.TargetBedId, nameof(request.TargetBedId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Muat entity PakaiBedModel (lama), verifikasi status Active
        var pakaiBedLama = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId))
            .GetValueOrThrow($"Pakai Bed '{request.PakaiBedId}' not found");

        if (pakaiBedLama.Periode.Keluar.Timestamp != new DateTime(3000, 1, 1) || pakaiBedLama.AuditTrail.IsVoided)
        {
            throw new InvalidOperationException($"Pakai Bed '{request.PakaiBedId}' is not active.");
        }

        var isBayiScope = string.Equals(request.TransferScope, "bayi", StringComparison.OrdinalIgnoreCase);

        // In baby scope, if request.PakaiBedId is mother's bed, we locate the rooming-in baby record
        PakaiBedModel? babyPakaiBed = null;
        if (isBayiScope)
        {
            if (pakaiBedLama.Tarif == 0 || (!string.IsNullOrWhiteSpace(request.BabyRegId) && pakaiBedLama.Reg.RegId == request.BabyRegId))
            {
                babyPakaiBed = pakaiBedLama;
            }
            else
            {
                var sourceBedId = (pakaiBedLama.Bed?.BedId ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(request.BabyRegId))
                {
                    var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(request.BabyRegId))?.ToList() ?? [];
                    babyPakaiBed = babyRecords.FirstOrDefault(x =>
                        (string.IsNullOrWhiteSpace(sourceBedId) || x.Bed.BedId == sourceBedId) &&
                        (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                }

                if (babyPakaiBed == null && !string.IsNullOrWhiteSpace(sourceBedId))
                {
                    var babyTrsIds = await _pakaiBedRepo.GetRoomingInBabyTrsIdsAsync(sourceBedId, request.BabyRegId);
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
                    var babyRegIds = await _pakaiBedRepo.GetRoomingInBabyRegIdsAsync(pakaiBedLama.PakaiBedId);
                    if (babyRegIds != null)
                    {
                        foreach (var bReg in babyRegIds)
                        {
                            var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(bReg))?.ToList() ?? [];
                            babyPakaiBed = babyRecords.FirstOrDefault(x =>
                                (string.IsNullOrWhiteSpace(sourceBedId) || x.Bed.BedId == sourceBedId) &&
                                (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                            if (babyPakaiBed != null) break;
                        }
                    }
                }

                if (babyPakaiBed == null)
                {
                    throw new InvalidOperationException($"Rooming-in baby record not found for bed '{pakaiBedLama.Bed?.BedId}'.");
                }
            }

            if (babyPakaiBed.Periode.Keluar.Timestamp != new DateTime(3000, 1, 1) || babyPakaiBed.AuditTrail.IsVoided)
            {
                throw new InvalidOperationException($"Pakai Bed '{babyPakaiBed.PakaiBedId}' is not active.");
            }
        }

        var subjectBedId = isBayiScope ? (babyPakaiBed!.Bed?.BedId ?? string.Empty).Trim() : (pakaiBedLama.Bed?.BedId ?? string.Empty).Trim();
        if (subjectBedId == request.TargetBedId)
        {
            throw new InvalidOperationException($"Target bed cannot be the same as current bed '{request.TargetBedId}'.");
        }

        // 2. Menolak transfer jika target bed sedang terisi
        var activeOccupant = _pakaiBedRepo.GetActiveOccupant(BedType.Key(request.TargetBedId));
        if (activeOccupant.HasValue)
        {
            throw new InvalidOperationException($"Target bed '{request.TargetBedId}' is already occupied.");
        }

        // 3. Load target bed and master entities
        var targetBed = _bedRepo.LoadEntity(BedType.Key(request.TargetBedId))
            .GetValueOrThrow($"Target bed '{request.TargetBedId}' not found");

        if (!targetBed.IsAktif)
        {
            throw new InvalidOperationException($"Target bed '{request.TargetBedId}' is not active on master.");
        }

        var targetKamar = _kamarRepo.LoadEntity(KamarType.Key(targetBed.Kamar.KamarId))
            .GetValueOrThrow($"Target kamar '{targetBed.Kamar.KamarId}' not found");

        var targetKelas = _kelasRepo.LoadEntity(KelasType.Key(targetKamar.Kelas.KelasId))
            .GetValueOrThrow($"Target kelas '{targetKamar.Kelas.KelasId}' not found");

        var roomRate = _roomRateRepo.LoadEntity(targetKamar)
            .GetValueOrThrow($"Room rate for kamar '{targetKamar.KamarId}' not found");

        var targetTipeKamarId = !string.IsNullOrWhiteSpace(request.TargetTipeKamarId)
            ? request.TargetTipeKamarId
            : (isBayiScope ? babyPakaiBed!.TipeKamar.TipeKamarId : pakaiBedLama.TipeKamar.TipeKamarId);

        var targetTipeKamar = _tipeKamarRepo.LoadEntity(TipeKamarType.Key(targetTipeKamarId))
            .GetValueOrThrow($"Tipe kamar '{targetTipeKamarId}' not found");

        var rate = roomRate.ListTipe.FirstOrDefault(x => x.TipeKamar.TipeKamarId == targetTipeKamarId)
            ?? throw new InvalidOperationException($"Room Rate for Tipe Kamar '{targetTipeKamarId}' not found in kamar '{targetKamar.KamarId}'.");

        string targetLayananId;
        var currentSubjectLayanan = isBayiScope ? babyPakaiBed!.Layanan.LayananId : pakaiBedLama.Layanan.LayananId;
        if (!string.IsNullOrWhiteSpace(request.TargetLayananId))
        {
            var maybeLayanan = _layananRepo.LoadEntity(LayananType.Key(request.TargetLayananId));
            targetLayananId = maybeLayanan.HasValue
                ? request.TargetLayananId
                : currentSubjectLayanan;
        }
        else
        {
            targetLayananId = currentSubjectLayanan;
        }

        var targetLayanan = _layananRepo.LoadEntity(LayananType.Key(targetLayananId))
            .GetValueOrThrow($"Layanan '{targetLayananId}' not found");

        // 4. Validasi status kesiapan operasional target bed
        var maybeTargetBedOp = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(request.TargetBedId));
        BedOperationalModel targetBedOperational;
        if (maybeTargetBedOp.HasValue)
        {
            targetBedOperational = maybeTargetBedOp.Value;
            if (targetBedOperational.CurrentReadiness.HasValue &&
                targetBedOperational.CurrentReadiness.Value != BedReadinessStatusEnum.Ready)
            {
                throw new InvalidOperationException(
                    $"Target bed '{request.TargetBedId}' is not Ready for allocation (status: {targetBedOperational.CurrentReadiness.Value}).");
            }

            if (targetBedOperational.HasUnresolvedBlocker)
            {
                throw new InvalidOperationException(
                    $"Target bed '{request.TargetBedId}' has unresolved blocker: {targetBedOperational.CurrentBlockerState.Reason}.");
            }
        }
        else
        {
            targetBedOperational = BedOperationalModel.Create(
                targetBed.BedId,
                targetBed.Bangsal.BangsalId,
                targetBed.Kamar.KamarId,
                OccupancyPolicyReff.Default);
        }

        var transferTime = SimulationTimeGuard.Resolve(
            request.TglJamTransfer,
            _tglJamProvider,
            operationName: "PakaiBedTransfer");
        var transferTimeUtc = transferTime.Kind == DateTimeKind.Utc 
            ? transferTime 
            : DateTime.SpecifyKind(transferTime, DateTimeKind.Utc);
        var businessNow = _tglJamProvider.Now;
        var nowUtc = businessNow.Kind == DateTimeKind.Utc
            ? businessNow
            : DateTime.SpecifyKind(businessNow, DateTimeKind.Utc);
        if (nowUtc < transferTimeUtc)
        {
            nowUtc = transferTimeUtc;
        }

        var periodeMasuk = new PeriodePakaiBedType(
            new AuditInfoType(request.UserId, transferTime),
            AuditInfoType.Default);

        if (isBayiScope)
        {
            // --- BRANCH 1: BAYI SCOPE (Isolated Child Transfer) ---
            var babyReg = _regRepo.LoadEntity(RegModel.Key(babyPakaiBed!.Reg.RegId))
                .Match(
                    onSome: r => r,
                    onNone: () => (RegModel)RegModel.Key(babyPakaiBed.Reg.RegId)
                );

            var babyPakaiBedBaru = PakaiBedModel.Create(
                babyReg,
                periodeMasuk.Masuk,
                targetLayanan,
                targetBed,
                targetTipeKamar,
                targetKelas,
                rate.TotalNilai);

            var oldBed = !string.IsNullOrWhiteSpace(subjectBedId)
                ? _bedRepo.LoadEntity(BedType.Key(subjectBedId))
                : MayBe<BedType>.None;
            var oldBangsalId = oldBed.HasValue ? oldBed.Value.Bangsal.BangsalId : targetBed.Bangsal.BangsalId;

            targetBedOperational = targetBedOperational.AdvanceOccupancyEpoch();

            using var trans = TransHelper.NewScope();

            // 1. Release baby's bed allocation on old bed
            babyPakaiBed.Release(request.UserId, transferTime);
            _pakaiBedRepo.SaveChanges(babyPakaiBed);

            if (!string.IsNullOrWhiteSpace(subjectBedId))
            {
                await _pakaiBedRepo.ReleaseRoomingInBabyAsync(subjectBedId, babyPakaiBed.Reg.RegId, request.UserId, transferTime);
            }

            // 2. Advance epoch on target bed operational
            _bedOperationalRepo.SaveChanges(targetBedOperational);

            // 3. Create independent baby bed allocation on target bed (with normal rate)
            _pakaiBedRepo.SaveChanges(babyPakaiBedBaru);

            // Note: Mother remains on the old bed! Old bed is NOT marked as CleaningRequired.
            trans.Complete();

            return new PakaiBedTransferResult(
                babyPakaiBedBaru.PakaiBedId,
                babyPakaiBed.PakaiBedId,
                targetBed.BedId,
                transferTime,
                SourceBedId: subjectBedId,
                SourceBangsalId: oldBangsalId,
                TargetBangsalId: targetBed.Bangsal.BangsalId,
                IsCleaningTriggered: false);
        }
        else
        {
            // --- BRANCH 2: IBU / DEFAULT SCOPE (Cascade Transfer with Rooming-In Babies) ---
            var reg = _regRepo.LoadEntity(RegModel.Key(pakaiBedLama.Reg.RegId))
                .GetValueOrThrow($"Registration '{pakaiBedLama.Reg.RegId}' not found");

            // 1. Prepare mother's new bed allocation on target bed
            var pakaiBedBaru = PakaiBedModel.Create(
                reg,
                periodeMasuk.Masuk,
                targetLayanan,
                targetBed,
                targetTipeKamar,
                targetKelas,
                rate.TotalNilai);

            // 2. Cascade transfer rooming-in babies on the same source bed (DISCOVER BEFORE MODIFYING DATABASE)
            var babyPakaiBedsToTransfer = new List<PakaiBedModel>();
            var processedBabyRegs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(request.BabyRegId))
            {
                var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(request.BabyRegId))?.ToList() ?? [];
                var activeBabyBed = babyRecords.FirstOrDefault(x =>
                    (string.IsNullOrWhiteSpace(subjectBedId) || x.Bed.BedId == subjectBedId) &&
                    (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                if (activeBabyBed != null)
                {
                    babyPakaiBedsToTransfer.Add(activeBabyBed);
                    processedBabyRegs.Add(activeBabyBed.Reg.RegId);
                }
            }

            if (!string.IsNullOrWhiteSpace(subjectBedId))
            {
                var babyTrsIds = await _pakaiBedRepo.GetRoomingInBabyTrsIdsAsync(subjectBedId, request.BabyRegId);
                if (babyTrsIds != null)
                {
                    foreach (var bTrsId in babyTrsIds)
                    {
                        if (bTrsId == pakaiBedLama.PakaiBedId) continue;
                        var loaded = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(bTrsId));
                        if (loaded.HasValue)
                        {
                            var bModel = loaded.Value;
                            if (!processedBabyRegs.Contains(bModel.Reg.RegId) &&
                                (bModel.Periode.Keluar.Timestamp == default || bModel.Periode.Keluar.Timestamp.Year == 3000))
                            {
                                babyPakaiBedsToTransfer.Add(bModel);
                                processedBabyRegs.Add(bModel.Reg.RegId);
                            }
                        }
                    }
                }
            }

            var detectedBabyRegs = await _pakaiBedRepo.GetRoomingInBabyRegIdsAsync(pakaiBedLama.PakaiBedId);
            if (detectedBabyRegs != null)
            {
                foreach (var bReg in detectedBabyRegs.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (processedBabyRegs.Contains(bReg.Trim())) continue;
                    var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(bReg.Trim()))?.ToList() ?? [];
                    var activeBabyBed = babyRecords.FirstOrDefault(x =>
                        (string.IsNullOrWhiteSpace(subjectBedId) || x.Bed.BedId == subjectBedId) &&
                        (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                    if (activeBabyBed != null)
                    {
                        babyPakaiBedsToTransfer.Add(activeBabyBed);
                        processedBabyRegs.Add(activeBabyBed.Reg.RegId);
                    }
                }
            }

            // Pre-load baby registration entities and construct baby models before entering transaction
            var babyTransferPairs = new List<(PakaiBedModel oldBabyBed, PakaiBedModel newBabyBed)>();
            foreach (var bBed in babyPakaiBedsToTransfer)
            {
                var babyReg = _regRepo.LoadEntity(RegModel.Key(bBed.Reg.RegId))
                    .Match(
                        onSome: r => r,
                        onNone: () => (RegModel)RegModel.Key(bBed.Reg.RegId)
                    );

                var babyPakaiBedBaru = PakaiBedModel.Create(
                    babyReg,
                    periodeMasuk.Masuk,
                    targetLayanan,
                    targetBed,
                    targetTipeKamar,
                    targetKelas,
                    0m, // Rooming-in baby has rate = 0
                    kelasTindakanId: pakaiBedBaru.KelasTindakanId);

                babyTransferPairs.Add((bBed, babyPakaiBedBaru));
            }

            // Pre-load bed operational entities before entering transaction
            var oldBed = !string.IsNullOrWhiteSpace(subjectBedId)
                ? _bedRepo.LoadEntity(BedType.Key(subjectBedId))
                : MayBe<BedType>.None;
            var oldBangsalId = oldBed.HasValue ? oldBed.Value.Bangsal.BangsalId : targetBed.Bangsal.BangsalId;
            var oldKamarId = oldBed.HasValue ? oldBed.Value.Kamar.KamarId : targetKamar.KamarId;

            var oldBedOperational = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(subjectBedId))
                .Match(
                    onSome: bo => bo,
                    onNone: () => BedOperationalModel.Create(
                        subjectBedId,
                        oldBangsalId,
                        oldKamarId,
                        OccupancyPolicyReff.Default)
                );

            var dirtyReason = string.IsNullOrWhiteSpace(request.AlasanPindah)
                ? $"Pindah kamar ke bed {request.TargetBedId}"
                : request.AlasanPindah;

            oldBedOperational = oldBedOperational.SetCleaningRequired(
                dirtyReason,
                request.UserId,
                transferTimeUtc,
                nowUtc,
                sourceFactId: $"TRANSFER-OUT-{pakaiBedLama.PakaiBedId}",
                requestId: $"REQ-{Guid.NewGuid():N}");

            targetBedOperational = targetBedOperational.AdvanceOccupancyEpoch();

            // 3. Atomic transaction: Writes only
            using var trans = TransHelper.NewScope();

            // Release mother's old bed
            pakaiBedLama.Release(request.UserId, transferTime);
            _pakaiBedRepo.SaveChanges(pakaiBedLama);

            // Cascade transfer rooming-in babies
            string? firstBabyPakaiBedBaruId = null;
            foreach (var (oldBabyBed, newBabyBed) in babyTransferPairs)
            {
                oldBabyBed.Release(request.UserId, transferTime);
                _pakaiBedRepo.SaveChanges(oldBabyBed);
                _pakaiBedRepo.SaveChanges(newBabyBed);
                firstBabyPakaiBedBaruId ??= newBabyBed.PakaiBedId;
            }

            if (!string.IsNullOrWhiteSpace(subjectBedId))
            {
                await _pakaiBedRepo.ReleaseRoomingInBabyAsync(subjectBedId, null, request.UserId, transferTime);
            }

            // Mark old bed as CleaningRequired
            _bedOperationalRepo.SaveChanges(oldBedOperational);

            // Advance epoch on target bed operational
            _bedOperationalRepo.SaveChanges(targetBedOperational);

            // Persist mother's new bed allocation on target bed
            _pakaiBedRepo.SaveChanges(pakaiBedBaru);

            trans.Complete();

            return new PakaiBedTransferResult(
                pakaiBedBaru.PakaiBedId,
                pakaiBedLama.PakaiBedId,
                targetBed.BedId,
                transferTime,
                SourceBedId: subjectBedId,
                SourceBangsalId: oldBangsalId,
                TargetBangsalId: targetBed.Bangsal.BangsalId,
                IsCleaningTriggered: true,
                BabyPakaiBedBaruId: firstBabyPakaiBedBaruId);
        }
    }
}
