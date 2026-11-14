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
using Bilreg.Domain.PasienContext.PasienFeature;
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
    public string? TransferScope { get; init; }
    public string? BabyRegId { get; init; }

    [JsonConstructor]
    public TransferToWardWaitingListCommand(
        string PakaiBedId,
        string TargetBangsalId,
        DateTime TglJamTransfer,
        string AlasanTransfer,
        string? Catatan = null,
        string? UserId = null,
        string? ClinicalUrgency = null,
        string? TransferScope = null,
        string? BabyRegId = null)
    {
        this.PakaiBedId = PakaiBedId;
        this.TargetBangsalId = TargetBangsalId;
        this.TglJamTransfer = TglJamTransfer;
        this.AlasanTransfer = AlasanTransfer;
        this.Catatan = Catatan;
        this.UserId = UserId ?? string.Empty;
        this.ClinicalUrgency = ClinicalUrgency;
        this.TransferScope = TransferScope;
        this.BabyRegId = BabyRegId;
    }
}

public record TransferToWardWaitingListResult(
    string WaitingListId,
    string PakaiBedId,
    string OldBedId,
    string OldBangsalId,
    string TargetBangsalId,
    DateTime TglJamTransfer,
    string Status = "Waiting",
    bool IsCleaningTriggered = true,
    string? BabyWaitingListId = null
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

    public async Task<TransferToWardWaitingListResult> Handle(TransferToWardWaitingListCommand request, CancellationToken cancellationToken)
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
        var oldBedId = (pakaiBed.Bed?.BedId ?? string.Empty).Trim();
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

        var isBayiScope = string.Equals(request.TransferScope, "bayi", StringComparison.OrdinalIgnoreCase);

        var referringUnit = new LayananReff(
            !string.IsNullOrWhiteSpace(oldBangsalId) ? oldBangsalId : pakaiBed.Layanan.LayananId,
            !string.IsNullOrWhiteSpace(oldBangsalName) ? oldBangsalName : pakaiBed.Layanan.LayananName);

        if (isBayiScope)
        {
            // --- BRANCH 1: BAYI SCOPE (Isolated Baby Waiting List Transfer) ---
            PakaiBedModel? babyPakaiBed = null;

            if (pakaiBed.Tarif == 0 || (!string.IsNullOrWhiteSpace(request.BabyRegId) && pakaiBed.Reg.RegId == request.BabyRegId))
            {
                babyPakaiBed = pakaiBed;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(request.BabyRegId))
                {
                    var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(request.BabyRegId))?.ToList() ?? [];
                    babyPakaiBed = babyRecords.FirstOrDefault(x =>
                        (string.IsNullOrWhiteSpace(oldBedId) || x.Bed.BedId == oldBedId) &&
                        (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                }

                if (babyPakaiBed == null && !string.IsNullOrWhiteSpace(oldBedId))
                {
                    var babyTrsIds = await _pakaiBedRepo.GetRoomingInBabyTrsIdsAsync(oldBedId, request.BabyRegId);
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
                                (string.IsNullOrWhiteSpace(oldBedId) || x.Bed.BedId == oldBedId) &&
                                (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                            if (babyPakaiBed != null) break;
                        }
                    }
                }

                if (babyPakaiBed == null && string.IsNullOrWhiteSpace(request.BabyRegId))
                {
                    throw new InvalidOperationException($"Rooming-in baby record not found for bed '{pakaiBed.Bed?.BedId}'.");
                }
            }

            if (babyPakaiBed != null && (babyPakaiBed.Periode.Keluar.Timestamp != new DateTime(3000, 1, 1) || babyPakaiBed.AuditTrail.IsVoided))
            {
                throw new InvalidOperationException($"Pakai Bed '{babyPakaiBed.PakaiBedId}' is not active.");
            }

            var babyRegId = babyPakaiBed?.Reg.RegId ?? request.BabyRegId!;
            var maybeBabyReg = _regRepo.LoadEntity(RegModel.Key(babyRegId));
            if (!maybeBabyReg.HasValue && babyPakaiBed == null)
            {
                throw new InvalidOperationException($"Registration '{babyRegId}' not found");
            }

            var babyReg = maybeBabyReg.HasValue ? maybeBabyReg.Value : null;
            var babyPasien = babyReg?.Pasien ?? new PasienReff(babyPakaiBed!.Reg.PasienId, babyPakaiBed.Reg.PasienName, DateOnly.MinValue, "-");
            var babyDoctor = babyReg?.Dokter != null
                ? new DokterReff(babyReg.Dokter.PpaId, babyReg.Dokter.PpaName)
                : new DokterReff("-", "-");

            using var trans = TransHelper.NewScope();

            // 1. Release baby bed allocation only
            if (babyPakaiBed != null)
            {
                babyPakaiBed.Release(request.UserId, transferTime);
                _pakaiBedRepo.SaveChanges(babyPakaiBed);
            }

            if (!string.IsNullOrWhiteSpace(oldBedId))
            {
                await _pakaiBedRepo.ReleaseRoomingInBabyAsync(oldBedId, babyRegId, request.UserId, transferTime);
            }

            // Note: Mother's bed allocation is NOT released.
            // Mother's bed is NOT marked as CleaningRequired.

            // 2. Reroute active waiting list for baby if exists
            var activeWl = _waitingListRepo.LoadActiveByRegId(babyRegId);
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

            var priorWl = activeWl.HasValue
                ? activeWl.Value
                : _waitingListRepo.ListByRegId(babyRegId)?.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(previousWlId) && priorWl != null)
            {
                previousWlId = priorWl.WaitingListId;
            }

            // 3. Clinical urgency
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

            var babyKelas = (babyReg?.Kelas != null && !string.IsNullOrWhiteSpace(babyReg.Kelas.KelasId) && babyReg.Kelas.KelasId != "-")
                ? babyReg.Kelas
                : (priorWl?.KelasRawat != null && !string.IsNullOrWhiteSpace(priorWl.KelasRawat.KelasId) && priorWl.KelasRawat.KelasId != "-"
                    ? priorWl.KelasRawat
                    : (babyPakaiBed?.Kelas ?? new KelasReff("-", "-")));

            var specialRequirements = !string.IsNullOrWhiteSpace(request.Catatan)
                ? $"{request.AlasanTransfer} - {request.Catatan}"
                : request.AlasanTransfer;

            var priority = (int)clinicalUrgency;

            var babyWaitingList = WaitingListModel.Create(
                babyRegId,
                babyPasien,
                babyKelas,
                targetBangsal.ToReff(),
                ArrivalSourceTypeEnum.TransferInternal,
                referringUnit,
                babyDoctor,
                clinicalUrgency,
                specialRequirements,
                priority: priority,
                auditUserId: request.UserId,
                createdAt: transferTime,
                previousWaitListId: previousWlId);

            _waitingListRepo.Save(babyWaitingList);

            trans.Complete();

            return new TransferToWardWaitingListResult(
                WaitingListId: babyWaitingList.WaitingListId,
                PakaiBedId: babyPakaiBed?.PakaiBedId ?? request.PakaiBedId,
                OldBedId: oldBedId,
                OldBangsalId: oldBangsalId,
                TargetBangsalId: targetBangsal.BangsalId,
                TglJamTransfer: transferTime,
                Status: babyWaitingList.WaitingListStatus.ToString(),
                IsCleaningTriggered: false,
                BabyWaitingListId: babyWaitingList.WaitingListId);
        }
        else
        {
            // --- BRANCH 2: IBU / DEFAULT SCOPE (Cascade Mother + Rooming-In Babies) ---
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

            // Fetch registration & patient data for mother
            var reg = _regRepo.LoadEntity(RegModel.Key(pakaiBed.Reg.RegId))
                .GetValueOrThrow($"Registration '{pakaiBed.Reg.RegId}' not found");

            // 1. Transition physical old bed readiness status to CleaningRequired (Housekeeping)
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

            // 2. Reroute or supersede existing active waiting list for mother
            var activeWl = _waitingListRepo.LoadActiveByRegId(reg.RegId);
            var previousWlId = string.Empty;
            WaitingListModel? reroutedMotherWl = null;
            if (activeWl.HasValue)
            {
                previousWlId = activeWl.Value.WaitingListId;
                reroutedMotherWl = activeWl.Value.Reroute(
                    $"Transfer ke bangsal {targetBangsal.BangsalName}",
                    request.UserId,
                    transferTime);
            }

            var priorWl = activeWl.HasValue
                ? activeWl.Value
                : _waitingListRepo.ListByRegId(reg.RegId)?.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(previousWlId) && priorWl != null)
            {
                previousWlId = priorWl.WaitingListId;
            }

            // 3. Clinical Urgency for mother
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

            // 4. Create new Waiting List ticket for mother in targetBangsalId
            var kelas = (reg.Kelas != null && !string.IsNullOrWhiteSpace(reg.Kelas.KelasId) && reg.Kelas.KelasId != "-")
                ? reg.Kelas
                : (priorWl?.KelasRawat != null && !string.IsNullOrWhiteSpace(priorWl.KelasRawat.KelasId) && priorWl.KelasRawat.KelasId != "-"
                    ? priorWl.KelasRawat
                    : (pakaiBed.Kelas ?? new KelasReff("-", "-")));

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

            // 5. Cascade rooming-in baby waitlist registration (DISCOVER BEFORE MODIFYING DATABASE)
            var babyPakaiBedsToTransfer = new List<PakaiBedModel>();
            var processedBabyRegs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(request.BabyRegId))
            {
                var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(request.BabyRegId))?.ToList() ?? [];
                var activeBabyBed = babyRecords.FirstOrDefault(x =>
                    (string.IsNullOrWhiteSpace(oldBedId) || x.Bed.BedId == oldBedId) &&
                    (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                if (activeBabyBed != null)
                {
                    babyPakaiBedsToTransfer.Add(activeBabyBed);
                    processedBabyRegs.Add(activeBabyBed.Reg.RegId);
                }
            }

            if (!string.IsNullOrWhiteSpace(oldBedId))
            {
                var babyTrsIds = await _pakaiBedRepo.GetRoomingInBabyTrsIdsAsync(oldBedId, request.BabyRegId);
                if (babyTrsIds != null)
                {
                    foreach (var bTrsId in babyTrsIds)
                    {
                        if (bTrsId == pakaiBed.PakaiBedId) continue;
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

            var detectedBabyRegs = await _pakaiBedRepo.GetRoomingInBabyRegIdsAsync(pakaiBed.PakaiBedId);
            if (detectedBabyRegs != null)
            {
                foreach (var bReg in detectedBabyRegs.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (processedBabyRegs.Contains(bReg.Trim())) continue;
                    var babyRecords = _pakaiBedRepo.ListData(RegModel.Key(bReg.Trim()))?.ToList() ?? [];
                    var activeBabyBed = babyRecords.FirstOrDefault(x =>
                        (string.IsNullOrWhiteSpace(oldBedId) || x.Bed.BedId == oldBedId) &&
                        (x.Periode.Keluar.Timestamp == default || x.Periode.Keluar.Timestamp.Year == 3000));
                    if (activeBabyBed != null)
                    {
                        babyPakaiBedsToTransfer.Add(activeBabyBed);
                        processedBabyRegs.Add(activeBabyBed.Reg.RegId);
                    }
                }
            }

            // Pre-load baby registration and waiting list models before entering transaction
            var babyWlDataList = new List<(PakaiBedModel bBed, WaitingListModel? reroutedBabyWl, WaitingListModel newBabyWl)>();
            foreach (var bBed in babyPakaiBedsToTransfer)
            {
                var maybeBabyReg = _regRepo.LoadEntity(RegModel.Key(bBed.Reg.RegId));
                var babyReg = maybeBabyReg.HasValue ? maybeBabyReg.Value : null;
                var babyPasien = babyReg?.Pasien ?? new PasienReff(bBed.Reg.PasienId, bBed.Reg.PasienName, DateOnly.MinValue, "-");
                var babyKelas = (babyReg?.Kelas != null && !string.IsNullOrWhiteSpace(babyReg.Kelas.KelasId) && babyReg.Kelas.KelasId != "-")
                    ? babyReg.Kelas
                    : (bBed.Kelas ?? kelas);
                var babyDoctor = babyReg?.Dokter != null
                    ? new DokterReff(babyReg.Dokter.PpaId, babyReg.Dokter.PpaName)
                    : referringDoctor;

                var activeBabyWl = _waitingListRepo.LoadActiveByRegId(bBed.Reg.RegId);
                var previousBabyWlId = string.Empty;
                WaitingListModel? reroutedWl = null;
                if (activeBabyWl.HasValue)
                {
                    previousBabyWlId = activeBabyWl.Value.WaitingListId;
                    reroutedWl = activeBabyWl.Value.Reroute(
                        $"Transfer ke bangsal {targetBangsal.BangsalName}",
                        request.UserId,
                        transferTime);
                }

                var priorBabyWl = activeBabyWl.HasValue
                    ? activeBabyWl.Value
                    : _waitingListRepo.ListByRegId(bBed.Reg.RegId)?.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(previousBabyWlId) && priorBabyWl != null)
                {
                    previousBabyWlId = priorBabyWl.WaitingListId;
                }

                var babyUrgency = priorBabyWl != null ? priorBabyWl.ClinicalUrgency : clinicalUrgency;
                var babyPriority = (int)babyUrgency;
                var babySpecialRequirements = !string.IsNullOrWhiteSpace(request.Catatan)
                    ? $"{request.AlasanTransfer} (Rawat Gabung) - {request.Catatan}"
                    : $"{request.AlasanTransfer} (Rawat Gabung)";

                var babyWaitingList = WaitingListModel.Create(
                    bBed.Reg.RegId,
                    babyPasien,
                    babyKelas,
                    targetBangsal.ToReff(),
                    ArrivalSourceTypeEnum.TransferInternal,
                    referringUnit,
                    babyDoctor,
                    babyUrgency,
                    babySpecialRequirements,
                    priority: babyPriority,
                    auditUserId: request.UserId,
                    createdAt: transferTime,
                    previousWaitListId: previousBabyWlId);

                babyWlDataList.Add((bBed, reroutedWl, babyWaitingList));
            }

            // 6. Atomic transaction: Writes only
            using var trans = TransHelper.NewScope();

            // Release old mother bed occupancy
            pakaiBed.Release(request.UserId, transferTime);
            _pakaiBedRepo.SaveChanges(pakaiBed);

            _bedOperationalRepo.SaveChanges(oldBedOperational);

            if (reroutedMotherWl != null)
            {
                _waitingListRepo.SaveChanges(reroutedMotherWl);
            }
            _waitingListRepo.Save(waitingList);

            string? firstBabyWaitingListId = null;

            foreach (var (bBed, reroutedBabyWl, newBabyWl) in babyWlDataList)
            {
                bBed.Release(request.UserId, transferTime);
                _pakaiBedRepo.SaveChanges(bBed);

                if (!string.IsNullOrWhiteSpace(oldBedId))
                {
                    await _pakaiBedRepo.ReleaseRoomingInBabyAsync(oldBedId, bBed.Reg.RegId, request.UserId, transferTime);
                }

                if (reroutedBabyWl != null)
                {
                    _waitingListRepo.SaveChanges(reroutedBabyWl);
                }

                _waitingListRepo.Save(newBabyWl);
                firstBabyWaitingListId ??= newBabyWl.WaitingListId;
            }

            if (!string.IsNullOrWhiteSpace(request.BabyRegId) && !processedBabyRegs.Contains(request.BabyRegId))
            {
                var maybeBabyReg = _regRepo.LoadEntity(RegModel.Key(request.BabyRegId));
                if (maybeBabyReg.HasValue)
                {
                    var babyReg = maybeBabyReg.Value;
                    if (!string.IsNullOrWhiteSpace(oldBedId))
                    {
                        await _pakaiBedRepo.ReleaseRoomingInBabyAsync(oldBedId, request.BabyRegId, request.UserId, transferTime);
                    }

                    var activeBabyWl = _waitingListRepo.LoadActiveByRegId(babyReg.RegId);
                    var previousBabyWlId = string.Empty;
                    if (activeBabyWl.HasValue)
                    {
                        previousBabyWlId = activeBabyWl.Value.WaitingListId;
                        var rerouted = activeBabyWl.Value.Reroute(
                            $"Transfer ke bangsal {targetBangsal.BangsalName}",
                            request.UserId,
                            transferTime);
                        _waitingListRepo.SaveChanges(rerouted);
                    }

                    var priorBabyWl = activeBabyWl.HasValue
                        ? activeBabyWl.Value
                        : _waitingListRepo.ListByRegId(babyReg.RegId)?.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(previousBabyWlId) && priorBabyWl != null)
                    {
                        previousBabyWlId = priorBabyWl.WaitingListId;
                    }

                    var babyUrgency = priorBabyWl != null ? priorBabyWl.ClinicalUrgency : clinicalUrgency;
                    var babyPriority = (int)babyUrgency;
                    var babySpecialRequirements = !string.IsNullOrWhiteSpace(request.Catatan)
                        ? $"{request.AlasanTransfer} (Rawat Gabung) - {request.Catatan}"
                        : $"{request.AlasanTransfer} (Rawat Gabung)";

                    var babyWaitingList = WaitingListModel.Create(
                        babyReg.RegId,
                        babyReg.Pasien,
                        babyReg.Kelas ?? kelas,
                        targetBangsal.ToReff(),
                        ArrivalSourceTypeEnum.TransferInternal,
                        referringUnit,
                        babyReg.Dokter != null ? new DokterReff(babyReg.Dokter.PpaId, babyReg.Dokter.PpaName) : referringDoctor,
                        babyUrgency,
                        babySpecialRequirements,
                        priority: babyPriority,
                        auditUserId: request.UserId,
                        createdAt: transferTime,
                        previousWaitListId: previousBabyWlId);

                    _waitingListRepo.Save(babyWaitingList);

                    firstBabyWaitingListId ??= babyWaitingList.WaitingListId;
                }
            }

            trans.Complete();

            return new TransferToWardWaitingListResult(
                WaitingListId: waitingList.WaitingListId,
                PakaiBedId: pakaiBed.PakaiBedId,
                OldBedId: oldBedId,
                OldBangsalId: oldBangsalId,
                TargetBangsalId: targetBangsal.BangsalId,
                TglJamTransfer: transferTime,
                Status: waitingList.WaitingListStatus.ToString(),
                IsCleaningTriggered: true,
                BabyWaitingListId: firstBabyWaitingListId);
        }
    }
}
