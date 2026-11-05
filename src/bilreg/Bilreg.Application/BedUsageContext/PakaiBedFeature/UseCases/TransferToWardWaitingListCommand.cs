using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;

public record TransferToWardWaitingListCommand : IRequest<TransferToWardWaitingListResult>
{
    public string PakaiBedId { get; init; } = string.Empty;
    public string TargetBangsalId { get; init; } = string.Empty;
    public DateTime TglJamTransfer { get; init; }
    public string AlasanTransfer { get; init; } = string.Empty;
    public string? Catatan { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? ClinicalUrgency { get; init; }

    [JsonConstructor]
    public TransferToWardWaitingListCommand(
        string PakaiBedId,
        string TargetBangsalId,
        DateTime TglJamTransfer,
        string AlasanTransfer,
        string? Catatan = null,
        string? UserId = null,
        string? ClinicalUrgency = null)
    {
        this.PakaiBedId = PakaiBedId;
        this.TargetBangsalId = TargetBangsalId;
        this.TglJamTransfer = TglJamTransfer;
        this.AlasanTransfer = AlasanTransfer;
        this.Catatan = Catatan;
        this.UserId = UserId ?? string.Empty;
        this.ClinicalUrgency = ClinicalUrgency;
    }
}

public record TransferToWardWaitingListResult(
    string WaitingListId,
    string PakaiBedId,
    string OldBedId,
    string OldBangsalId,
    string TargetBangsalId,
    DateTime TglJamTransfer,
    string Status = "Waiting"
);

public class TransferToWardWaitingListCommandHandler : IRequestHandler<TransferToWardWaitingListCommand, TransferToWardWaitingListResult>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IBangsalRepo _bangsalRepo;
    private readonly IBedRepo _bedRepo;
    private readonly IRegRepo _regRepo;
    private readonly IBedOperationalRepo _bedOperationalRepo;
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TransferToWardWaitingListCommandHandler(
        IPakaiBedRepo pakaiBedRepo,
        IBangsalRepo bangsalRepo,
        IBedRepo bedRepo,
        IRegRepo regRepo,
        IBedOperationalRepo bedOperationalRepo,
        IWaitingListRepo waitingListRepo,
        ITglJamProvider tglJamProvider)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _bangsalRepo = bangsalRepo;
        _bedRepo = bedRepo;
        _regRepo = regRepo;
        _bedOperationalRepo = bedOperationalRepo;
        _waitingListRepo = waitingListRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TransferToWardWaitingListResult> Handle(TransferToWardWaitingListCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.TargetBangsalId, nameof(request.TargetBangsalId));
        Guard.Against.NullOrWhiteSpace(request.AlasanTransfer, nameof(request.AlasanTransfer));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Fetch active bed occupancy for pakaiBedId
        var pakaiBed = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId))
            .GetValueOrThrow($"Pakai Bed '{request.PakaiBedId}' not found");

        if (pakaiBed.Periode.Keluar.Timestamp != new DateTime(3000, 1, 1) || pakaiBed.AuditTrail.IsVoided)
        {
            throw new InvalidOperationException($"Pakai Bed '{request.PakaiBedId}' is not active.");
        }

        // 2. Fetch target bangsal
        var targetBangsal = _bangsalRepo.LoadEntity(BangsalType.Key(request.TargetBangsalId))
            .GetValueOrThrow($"Bangsal '{request.TargetBangsalId}' not found");

        // 3. Resolve old bed and old bangsal info
        var oldBedId = pakaiBed.Bed.BedId;
        var oldBed = _bedRepo.LoadEntity(BedType.Key(oldBedId));
        var oldBangsalId = oldBed.HasValue ? oldBed.Value.Bangsal.BangsalId : pakaiBed.Layanan.LayananId;
        var oldBangsalName = oldBed.HasValue ? oldBed.Value.Bangsal.BangsalName : pakaiBed.Layanan.LayananName;
        var oldKamarId = oldBed.HasValue ? oldBed.Value.Kamar.KamarId : "-";

        if (!string.IsNullOrWhiteSpace(oldBangsalId) && oldBangsalId == request.TargetBangsalId)
        {
            throw new InvalidOperationException("Transfer antar-bangsal harus memilih bangsal yang berbeda dengan bangsal saat ini.");
        }

        // 4. Resolve simulation/transfer time
        var transferTime = SimulationTimeGuard.Resolve(
            request.TglJamTransfer,
            _tglJamProvider,
            operationName: "TransferToWardWaitingList");

        if (pakaiBed.Periode.Masuk.Timestamp != default && transferTime < pakaiBed.Periode.Masuk.Timestamp)
        {
            if (transferTime.Date == pakaiBed.Periode.Masuk.Timestamp.Date)
            {
                transferTime = pakaiBed.Periode.Masuk.Timestamp;
            }
            else
            {
                throw new InvalidOperationException("Waktu transfer tidak boleh lebih awal dari waktu masuk.");
            }
        }

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

        // 5. Fetch registration & patient data
        var reg = _regRepo.LoadEntity(RegModel.Key(pakaiBed.Reg.RegId))
            .GetValueOrThrow($"Registration '{pakaiBed.Reg.RegId}' not found");

        // 6. Execute atomic persistence
        using var trans = TransHelper.NewScope();

        // 6a. Release old bed occupancy
        pakaiBed.Release(request.UserId, transferTime);
        _pakaiBedRepo.SaveChanges(pakaiBed);

        // 6b. Transition physical old bed readiness status to CleaningRequired (Housekeeping)
        var oldBedOperational = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(oldBedId))
            .Match(
                onSome: bo => bo,
                onNone: () => BedOperationalModel.Create(
                    oldBedId,
                    oldBangsalId,
                    oldKamarId,
                    OccupancyPolicyReff.Default)
            );

        var dirtyReason = $"Transfer Bangsal ke {targetBangsal.BangsalName}: {request.AlasanTransfer}";

        oldBedOperational = oldBedOperational.SetCleaningRequired(
            dirtyReason,
            request.UserId,
            transferTimeUtc,
            nowUtc,
            sourceFactId: $"TRANSFER-OUT-{pakaiBed.PakaiBedId}",
            requestId: $"REQ-{Guid.NewGuid():N}");

        _bedOperationalRepo.SaveChanges(oldBedOperational);

        // 6c. If there is an existing active waiting list, reroute or supersede it
        var activeWl = _waitingListRepo.LoadActiveByRegId(reg.RegId);
        var previousWlId = string.Empty;
        if (activeWl.HasValue)
        {
            previousWlId = activeWl.Value.WaitingListId;
            var rerouted = activeWl.Value.Reroute(
                $"Transfer ke bangsal {targetBangsal.BangsalName}",
                request.UserId,
                transferTime);
            _waitingListRepo.SaveChanges(rerouted);
        }

        // Cari waiting list sebelumnya untuk mempertahankan urgenitas klinis asli (Cito/Urgent/Elektif)
        var priorWl = activeWl.HasValue
            ? activeWl.Value
            : _waitingListRepo.ListByRegId(reg.RegId)?.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(previousWlId) && priorWl != null)
        {
            previousWlId = priorWl.WaitingListId;
        }

        // Tentukan clinicalUrgency:
        // 1. Jika request mengirim ClinicalUrgency eksplisit, gunakan itu
        // 2. Jika tidak, warisi urgenitas dari waiting list sebelumnya (Cito tetap Cito)
        // 3. Fallback jika tidak ada record sebelumnya: deteksi dari kata kunci pada AlasanTransfer
        // 4. Default ke Routine (Elektif)
        ClinicalUrgencyEnum clinicalUrgency;
        if (!string.IsNullOrWhiteSpace(request.ClinicalUrgency))
        {
            var u = request.ClinicalUrgency.Trim().ToLowerInvariant();
            clinicalUrgency = u switch
            {
                "cito" or "emergency" or "2" => ClinicalUrgencyEnum.Emergency,
                "urgent" or "1" => ClinicalUrgencyEnum.Urgent,
                _ => ClinicalUrgencyEnum.Routine
            };
        }
        else if (priorWl != null)
        {
            clinicalUrgency = priorWl.ClinicalUrgency;
        }
        else if (request.AlasanTransfer.Contains("Cito", StringComparison.OrdinalIgnoreCase)
                 || request.AlasanTransfer.Contains("Emergency", StringComparison.OrdinalIgnoreCase))
        {
            clinicalUrgency = ClinicalUrgencyEnum.Emergency;
        }
        else if (request.AlasanTransfer.Contains("ICU", StringComparison.OrdinalIgnoreCase)
                 || request.AlasanTransfer.Contains("Urgent", StringComparison.OrdinalIgnoreCase))
        {
            clinicalUrgency = ClinicalUrgencyEnum.Urgent;
        }
        else
        {
            clinicalUrgency = ClinicalUrgencyEnum.Routine;
        }

        // 6d. Create new Waiting List ticket for targetBangsalId
        var kelas = (pakaiBed.Kelas != null && !string.IsNullOrWhiteSpace(pakaiBed.Kelas.KelasId) && pakaiBed.Kelas.KelasId != "-")
            ? pakaiBed.Kelas
            : reg.Kelas;

        var referringUnit = new LayananReff(
            !string.IsNullOrWhiteSpace(oldBangsalId) ? oldBangsalId : pakaiBed.Layanan.LayananId,
            !string.IsNullOrWhiteSpace(oldBangsalName) ? oldBangsalName : pakaiBed.Layanan.LayananName);

        var referringDoctor = reg.Dokter != null
            ? new DokterReff(reg.Dokter.PpaId, reg.Dokter.PpaName)
            : new DokterReff("-", "-");

        var specialRequirements = !string.IsNullOrWhiteSpace(request.Catatan)
            ? $"{request.AlasanTransfer} - {request.Catatan}"
            : request.AlasanTransfer;

        var priority = (int)clinicalUrgency;

        var waitingList = WaitingListModel.Create(
            reg.RegId,
            reg.Pasien,
            kelas,
            targetBangsal.ToReff(),
            ArrivalSourceTypeEnum.TransferInternal,
            referringUnit,
            referringDoctor,
            clinicalUrgency,
            specialRequirements,
            priority: priority,
            auditUserId: request.UserId,
            createdAt: transferTime,
            previousWaitListId: previousWlId);

        _waitingListRepo.Save(waitingList);

        trans.Complete();

        return Task.FromResult(new TransferToWardWaitingListResult(
            WaitingListId: waitingList.WaitingListId,
            PakaiBedId: pakaiBed.PakaiBedId,
            OldBedId: oldBedId,
            OldBangsalId: oldBangsalId,
            TargetBangsalId: targetBangsal.BangsalId,
            TglJamTransfer: transferTime,
            Status: waitingList.WaitingListStatus.ToString()));
    }
}
