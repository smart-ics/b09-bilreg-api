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

    public string PakaiBedLamaId => PakaiBedId;

    [JsonConstructor]
    public PakaiBedTransferCommand(
        string PakaiBedId,
        string TargetBedId,
        string UserId,
        DateTime TglJamTransfer = default,
        string? AlasanPindah = null,
        string? TargetTipeKamarId = null,
        string? TargetLayananId = null)
    {
        this.PakaiBedId = PakaiBedId;
        this.TargetBedId = TargetBedId;
        this.UserId = UserId;
        this.TglJamTransfer = TglJamTransfer;
        this.AlasanPindah = AlasanPindah;
        this.TargetTipeKamarId = TargetTipeKamarId;
        this.TargetLayananId = TargetLayananId;
    }

    public PakaiBedTransferCommand(
        string PakaiBedId,
        string TargetBedId,
        string UserId) 
        : this(PakaiBedId, TargetBedId, UserId, default, null, null, null)
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
    string TargetBangsalId = ""
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

    public Task<PakaiBedTransferResult> Handle(PakaiBedTransferCommand request, CancellationToken cancellationToken)
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

        if (pakaiBedLama.Bed.BedId == request.TargetBedId)
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
            : pakaiBedLama.TipeKamar.TipeKamarId;

        var targetTipeKamar = _tipeKamarRepo.LoadEntity(TipeKamarType.Key(targetTipeKamarId))
            .GetValueOrThrow($"Tipe kamar '{targetTipeKamarId}' not found");

        var rate = roomRate.ListTipe.FirstOrDefault(x => x.TipeKamar.TipeKamarId == targetTipeKamarId)
            ?? throw new InvalidOperationException($"Room Rate for Tipe Kamar '{targetTipeKamarId}' not found in kamar '{targetKamar.KamarId}'.");

        string targetLayananId;
        if (!string.IsNullOrWhiteSpace(request.TargetLayananId))
        {
            var maybeLayanan = _layananRepo.LoadEntity(LayananType.Key(request.TargetLayananId));
            targetLayananId = maybeLayanan.HasValue
                ? request.TargetLayananId
                : pakaiBedLama.Layanan.LayananId;
        }
        else
        {
            targetLayananId = pakaiBedLama.Layanan.LayananId;
        }

        var targetLayanan = _layananRepo.LoadEntity(LayananType.Key(targetLayananId))
            .GetValueOrThrow($"Layanan '{targetLayananId}' not found");

        var reg = _regRepo.LoadEntity(RegModel.Key(pakaiBedLama.Reg.RegId))
            .GetValueOrThrow($"Registration '{pakaiBedLama.Reg.RegId}' not found");

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

        // 5. Atomic transaction
        using var trans = TransHelper.NewScope();

        // 5a. Release bed lama
        pakaiBedLama.Release(request.UserId, transferTime);
        _pakaiBedRepo.SaveChanges(pakaiBedLama);

        // 5b. Otomatis mencatat bed lama menjadi berstatus kotor (CleaningRequired) via BedOperationalModel
        var oldBed = _bedRepo.LoadEntity(BedType.Key(pakaiBedLama.Bed.BedId));
        var oldBangsalId = oldBed.HasValue ? oldBed.Value.Bangsal.BangsalId : targetBed.Bangsal.BangsalId;
        var oldKamarId = oldBed.HasValue ? oldBed.Value.Kamar.KamarId : targetKamar.KamarId;

        var oldBedOperational = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(pakaiBedLama.Bed.BedId))
            .Match(
                onSome: bo => bo,
                onNone: () => BedOperationalModel.Create(
                    pakaiBedLama.Bed.BedId,
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

        _bedOperationalRepo.SaveChanges(oldBedOperational);

        // 5c. Advance epoch on target bed operational
        targetBedOperational = targetBedOperational.AdvanceOccupancyEpoch();
        _bedOperationalRepo.SaveChanges(targetBedOperational);

        // 5d. Create alokasi bed baru
        var periodeMasuk = new PeriodePakaiBedType(
            new AuditInfoType(request.UserId, transferTime),
            AuditInfoType.Default);

        var pakaiBedBaru = PakaiBedModel.Create(
            reg,
            periodeMasuk.Masuk,
            targetLayanan,
            targetBed,
            targetTipeKamar,
            targetKelas,
            rate.TotalNilai);

        _pakaiBedRepo.SaveChanges(pakaiBedBaru);

        trans.Complete();

        return Task.FromResult(new PakaiBedTransferResult(
            pakaiBedBaru.PakaiBedId,
            pakaiBedLama.PakaiBedId,
            targetBed.BedId,
            transferTime,
            SourceBedId: pakaiBedLama.Bed.BedId,
            SourceBangsalId: oldBangsalId,
            TargetBangsalId: targetBed.Bangsal.BangsalId));
    }
}
