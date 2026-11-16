using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.Integration;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases
{
    public record PakaiBedVoidCommand(
        string PakaiBedId, string UserId, string VoidReason, string UserAgent, string RemoteIpAddress, string? CancelScope = null)
        : IRequest, IPakaiBed;

    public class PakaiBedDeleteCommandHandler : IRequestHandler<PakaiBedVoidCommand>
    {
        private readonly IPakaiBedRepo _pakaiBedRepo;
        private readonly IAuditRepo _auditRepo;
        private readonly ITglJamProvider _tglJamProvider;
        private readonly IPakaiBedBillingLockChecker? _billingLockChecker;
        private readonly IPakaiBedDal? _pakaiBedDal;
        private readonly ITindakanRepo? _tindakanRepo;
        private readonly IWaitingListRepo? _waitingListRepo;
        private readonly IRegRepo? _regRepo;
        private readonly IBedRepo? _bedRepo;
        private readonly IBangsalRepo? _bangsalRepo;
        private readonly IBedOperationalRepo? _bedOperationalRepo;
        private readonly IWardCockpitEventPublisher? _eventPublisher;
        private readonly IEmr20AntrianRanapGateway? _emr20Gateway;

        public PakaiBedDeleteCommandHandler(
            IPakaiBedRepo pakaiBedRepo, 
            IAuditRepo auditRepo, 
            ITglJamProvider tglJamProvider,
            IPakaiBedBillingLockChecker? billingLockChecker = null,
            IPakaiBedDal? pakaiBedDal = null,
            ITindakanRepo? tindakanRepo = null,
            IWaitingListRepo? waitingListRepo = null,
            IRegRepo? regRepo = null,
            IBedRepo? bedRepo = null,
            IBangsalRepo? bangsalRepo = null,
            IBedOperationalRepo? bedOperationalRepo = null,
            IWardCockpitEventPublisher? eventPublisher = null,
            IEmr20AntrianRanapGateway? emr20Gateway = null)
        {
            _pakaiBedRepo = pakaiBedRepo;
            _auditRepo = auditRepo;
            _tglJamProvider = tglJamProvider;
            _billingLockChecker = billingLockChecker;
            _pakaiBedDal = pakaiBedDal;
            _tindakanRepo = tindakanRepo;
            _waitingListRepo = waitingListRepo;
            _regRepo = regRepo;
            _bedRepo = bedRepo;
            _bangsalRepo = bangsalRepo;
            _bedOperationalRepo = bedOperationalRepo;
            _eventPublisher = eventPublisher;
            _emr20Gateway = emr20Gateway;
        }

        public async Task Handle(PakaiBedVoidCommand request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.PakaiBedId);
            Guard.Against.NullOrWhiteSpace(request.UserId);
            Guard.Against.NullOrWhiteSpace(request.VoidReason);

            var saved = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId))
                .GetValueOrThrow($"Trs. Pakai Bed {request.PakaiBedId} not found");

            // Check if cancellation scope is for baby only
            if (string.Equals(request.CancelScope, "bayi", StringComparison.OrdinalIgnoreCase))
            {
                await HandleCancelBabyScopeAsync(saved, request, cancellationToken);
                return;
            }

            // 1. Billing Lock Check
            if (_billingLockChecker != null)
            {
                if (_billingLockChecker.CheckIsBillingLocked(saved.Reg.RegId))
                {
                    throw new BillingLockedException(saved.Reg.RegId);
                }
            }

            // 2. Note: ITindakanRepo is intentionally NOT blocking and NOT modifying ta_trs_tindakan.
            // Clinical procedures are strictly preserved per user requirement:
            // "jangan hapus tindakan yang telah di input, cukup hapus room charge pada bed yang bersangkutan saat batal check-in bed bangsal rawat inap"

            var snapshotJson = AuditLogSnapshotJson.Serialize(saved);
            var occurredAt = _tglJamProvider.Now;
            var affectedBangsalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 3. Update Domain Model state and persist
            saved.Void(request.UserId, occurredAt);
            _pakaiBedRepo.SaveChanges(saved);

            if (_emr20Gateway != null)
            {
                await _emr20Gateway.DischargeRegRanapAsync(saved.Reg.RegId, isGabung: false, cancellationToken);
            }

            // 4. Atomic Void on Bed & Room Charge in DAL
            if (_pakaiBedDal != null)
            {
                var dalBangsals = await _pakaiBedDal.VoidBedAndRoomChargeAsync(request.PakaiBedId, request.UserId, request.VoidReason, occurredAt);
                if (dalBangsals != null)
                {
                    foreach (var b in dalBangsals.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        affectedBangsalIds.Add(b.Trim());
                    }
                }
            }

            // 5. Kembalikan pasien ke Waiting List
            if (_waitingListRepo != null)
            {
                BangsalReff? targetBangsal = null;
                if (_bedRepo != null && _bangsalRepo != null && !string.IsNullOrWhiteSpace(saved.Bed?.BedId))
                {
                    var maybeBed = _bedRepo.LoadEntity(BedType.Key(saved.Bed.BedId));
                    if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
                    {
                        var maybeBsl = _bangsalRepo.LoadEntity(BangsalType.Key(maybeBed.Value.Bangsal.BangsalId));
                        if (maybeBsl.HasValue)
                        {
                            targetBangsal = maybeBsl.Value.ToReff();
                        }
                    }
                }
                targetBangsal ??= new BangsalReff(saved.Layanan.LayananId, saved.Layanan.LayananName);

                // 5a. Direct patient reversal / fallback
                var listWl = _waitingListRepo.ListByRegId(saved.Reg.RegId)?.ToList() ?? [];
                var matchedWl = listWl.FirstOrDefault(x => x.PlacedPakaiBedId == request.PakaiBedId)
                             ?? listWl.FirstOrDefault(x => x.WaitingListStatus == WaitingListStatusEnum.Placed);

                if (matchedWl != null)
                {
                    var revertedWl = matchedWl.RevertToWaiting(request.UserId, occurredAt);
                    _waitingListRepo.SaveChanges(revertedWl);
                    if (!string.IsNullOrWhiteSpace(revertedWl.Bangsal?.BangsalId))
                    {
                        affectedBangsalIds.Add(revertedWl.Bangsal.BangsalId.Trim());
                    }
                }
                else if (!listWl.Any(x => x.WaitingListStatus == WaitingListStatusEnum.Waiting || x.WaitingListStatus == WaitingListStatusEnum.Reserved))
                {
                    if (_regRepo != null)
                    {
                        var maybeReg = _regRepo.LoadEntity(RegModel.Key(saved.Reg.RegId));
                        if (maybeReg.HasValue && maybeReg.Value.IsAktif)
                        {
                            var reg = maybeReg.Value;
                            var isBaby = saved.Tarif == 0
                                || !string.IsNullOrWhiteSpace(reg.RegIbu)
                                || reg.JenisInap?.JenisInapId == "3";

                            var note = isBaby
                                ? "Batal Check-in Rawat Gabung"
                                : $"Batal Check-in: {request.VoidReason}";

                            var wlKelas = reg.Kelas != null && !string.IsNullOrWhiteSpace(reg.Kelas.KelasId) && reg.Kelas.KelasId != "-"
                                ? reg.Kelas
                                : (saved.Kelas ?? new KelasReff("-", "-"));

                            var newWl = WaitingListModel.Create(
                                reg.RegId,
                                reg.Pasien,
                                wlKelas,
                                targetBangsal,
                                ArrivalSourceTypeEnum.Adm,
                                saved.Layanan != null ? new LayananReff(saved.Layanan.LayananId, saved.Layanan.LayananName) : new LayananReff("-", "-"),
                                reg.Dokter != null ? new DokterReff(reg.Dokter.PpaId, reg.Dokter.PpaName) : new DokterReff("-", "-"),
                                ClinicalUrgencyEnum.Routine,
                                note,
                                priority: 0,
                                auditUserId: request.UserId,
                                createdAt: occurredAt);

                            _waitingListRepo.Save(newWl);
                            if (!string.IsNullOrWhiteSpace(newWl.Bangsal?.BangsalId))
                            {
                                affectedBangsalIds.Add(newWl.Bangsal.BangsalId.Trim());
                            }
                        }
                    }
                }

                // 5b. Rooming-in baby waiting list handling & fallback
                var babyRegIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (_pakaiBedDal != null)
                {
                    var dalBabies = await _pakaiBedDal.GetRoomingInBabyRegIdsAsync(request.PakaiBedId);
                    if (dalBabies != null)
                    {
                        foreach (var bReg in dalBabies.Where(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            babyRegIds.Add(bReg.Trim());
                        }
                    }
                }
                else if (_pakaiBedRepo != null)
                {
                    var repoBabies = await _pakaiBedRepo.GetRoomingInBabyRegIdsAsync(request.PakaiBedId);
                    if (repoBabies != null)
                    {
                        foreach (var bReg in repoBabies.Where(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            babyRegIds.Add(bReg.Trim());
                        }
                    }
                }

                foreach (var babyRegId in babyRegIds)
                {
                    if (string.Equals(babyRegId, saved.Reg.RegId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var babyWlList = _waitingListRepo.ListByRegId(babyRegId)?.ToList() ?? [];
                    var activeBabyWl = babyWlList.FirstOrDefault(x => x.WaitingListStatus == WaitingListStatusEnum.Waiting);

                    if (activeBabyWl != null)
                    {
                        if (!string.IsNullOrWhiteSpace(activeBabyWl.Bangsal?.BangsalId))
                        {
                            affectedBangsalIds.Add(activeBabyWl.Bangsal.BangsalId.Trim());
                        }
                    }
                    else
                    {
                        var placedBabyWl = babyWlList.FirstOrDefault(x => x.WaitingListStatus == WaitingListStatusEnum.Placed);
                        if (placedBabyWl != null)
                        {
                            var revertedBabyWl = placedBabyWl.RevertToWaiting(request.UserId, occurredAt);
                            _waitingListRepo.SaveChanges(revertedBabyWl);
                            if (!string.IsNullOrWhiteSpace(revertedBabyWl.Bangsal?.BangsalId))
                            {
                                affectedBangsalIds.Add(revertedBabyWl.Bangsal.BangsalId.Trim());
                            }
                        }
                        else if (!babyWlList.Any(x => x.WaitingListStatus == WaitingListStatusEnum.Waiting || x.WaitingListStatus == WaitingListStatusEnum.Reserved))
                        {
                            if (_regRepo != null)
                            {
                                var maybeBabyReg = _regRepo.LoadEntity(RegModel.Key(babyRegId));
                                if (maybeBabyReg.HasValue && maybeBabyReg.Value.IsAktif)
                                {
                                    var babyReg = maybeBabyReg.Value;
                                    var babyKelas = babyReg.Kelas != null && babyReg.Kelas.KelasId != "-"
                                        ? babyReg.Kelas
                                        : (saved.Kelas != null && saved.Kelas.KelasId != "-" ? saved.Kelas : new KelasReff("-", "-"));
                                    var babyDokter = babyReg.Dokter != null
                                        ? new DokterReff(babyReg.Dokter.PpaId, babyReg.Dokter.PpaName)
                                        : new DokterReff("-", "-");
                                    var babyLayanan = saved.Layanan != null
                                        ? new LayananReff(saved.Layanan.LayananId, saved.Layanan.LayananName)
                                        : new LayananReff("-", "-");

                                    var fallbackBabyWl = WaitingListModel.Create(
                                        babyReg.RegId,
                                        babyReg.Pasien,
                                        babyKelas,
                                        targetBangsal,
                                        ArrivalSourceTypeEnum.Adm,
                                        babyLayanan,
                                        babyDokter,
                                        ClinicalUrgencyEnum.Routine,
                                        "Batal Check-in Rawat Gabung",
                                        priority: 0,
                                        auditUserId: request.UserId,
                                        createdAt: occurredAt);

                                    _waitingListRepo.Save(fallbackBabyWl);
                                    if (!string.IsNullOrWhiteSpace(fallbackBabyWl.Bangsal?.BangsalId))
                                    {
                                        affectedBangsalIds.Add(fallbackBabyWl.Bangsal.BangsalId.Trim());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 6. Transisi BedOperational ke status kotor / House Keeping (CleaningRequired)
            if (_bedOperationalRepo != null)
            {
                var occurredAtUtc = occurredAt.Kind == DateTimeKind.Utc
                    ? occurredAt
                    : (occurredAt.Kind == DateTimeKind.Local
                        ? occurredAt.ToUniversalTime()
                        : DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc));

                var businessNow = _tglJamProvider.Now;
                var nowUtc = businessNow.Kind == DateTimeKind.Utc
                    ? businessNow
                    : (businessNow.Kind == DateTimeKind.Local
                        ? businessNow.ToUniversalTime()
                        : DateTime.SpecifyKind(businessNow, DateTimeKind.Utc));
                if (nowUtc < occurredAtUtc)
                {
                    nowUtc = occurredAtUtc;
                }

                var maybeBedOp = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(saved.Bed.BedId));
                var bedOperational = maybeBedOp.Match(
                    onSome: bo => bo,
                    onNone: () =>
                    {
                        var bangsalId = saved.Layanan?.LayananId ?? "DEFAULT";
                        var kamarId = "DEFAULT";
                        if (_bedRepo != null)
                        {
                            var maybeBed = _bedRepo.LoadEntity(BedType.Key(saved.Bed.BedId));
                            if (maybeBed.HasValue)
                            {
                                bangsalId = maybeBed.Value.Bangsal?.BangsalId ?? bangsalId;
                                kamarId = maybeBed.Value.Kamar?.KamarId ?? kamarId;
                            }
                        }
                        return BedOperationalModel.Create(
                            saved.Bed.BedId,
                            bangsalId,
                            kamarId,
                            OccupancyPolicyReff.Default);
                    });

                var latestTxOccurredAtUtc = bedOperational.ListReadinessTransaction
                    .OrderBy(x => x.OccurredAt)
                    .LastOrDefault()?.OccurredAt ?? DateTime.MinValue;

                if (occurredAtUtc <= latestTxOccurredAtUtc)
                {
                    var nextOccurred = latestTxOccurredAtUtc.AddSeconds(1);
                    occurredAtUtc = nextOccurred.Kind == DateTimeKind.Utc
                        ? nextOccurred
                        : DateTime.SpecifyKind(nextOccurred, DateTimeKind.Utc);
                }
                if (nowUtc < occurredAtUtc)
                {
                    nowUtc = occurredAtUtc;
                }

                var pkbPart = request.PakaiBedId.Length > 10 ? request.PakaiBedId[..10] : request.PakaiBedId;
                var shortGuid = Guid.NewGuid().ToString("N")[..8];
                var factId = $"FACT-VOID-{pkbPart}-{shortGuid}";
                if (factId.Length > 50)
                {
                    factId = factId[..50];
                }
                var reqId = $"REQ-{shortGuid}";

                var reason = $"Pembersihan Pasca Batal Check-In: {request.VoidReason}";

                var bedOp = bedOperational.SetCleaningRequired(
                    reason,
                    request.UserId,
                    occurredAtUtc,
                    nowUtc,
                    sourceFactId: factId,
                    requestId: reqId);
                _bedOperationalRepo.SaveChanges(bedOp);
            }

            // 7. Notifikasi SignalR ke Ward Cockpit
            if (_eventPublisher != null)
            {
                var bangsalId = string.Empty;
                if (_bedRepo != null && !string.IsNullOrWhiteSpace(saved.Bed.BedId))
                {
                    var maybeBed = _bedRepo.LoadEntity(BedType.Key(saved.Bed.BedId));
                    if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
                    {
                        bangsalId = maybeBed.Value.Bangsal.BangsalId;
                    }
                }

                if (string.IsNullOrWhiteSpace(bangsalId) && _bangsalRepo != null && !string.IsNullOrWhiteSpace(saved.Layanan.LayananId))
                {
                    var maybeDirectBangsal = _bangsalRepo.LoadEntity(BangsalType.Key(saved.Layanan.LayananId));
                    if (maybeDirectBangsal.HasValue && !string.IsNullOrWhiteSpace(maybeDirectBangsal.Value.BangsalId) && maybeDirectBangsal.Value.BangsalId != "-")
                    {
                        bangsalId = maybeDirectBangsal.Value.BangsalId;
                    }
                    else
                    {
                        var listBangsal = _bangsalRepo.ListData(LayananType.Key(saved.Layanan.LayananId));
                        var firstBangsal = listBangsal?.FirstOrDefault();
                        if (firstBangsal != null && !string.IsNullOrWhiteSpace(firstBangsal.BangsalId) && firstBangsal.BangsalId != "-")
                        {
                            bangsalId = firstBangsal.BangsalId;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(bangsalId))
                {
                    bangsalId = saved.Layanan.LayananId;
                }

                var voidBedId = (saved.Bed?.BedId ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(bangsalId) && !string.IsNullOrWhiteSpace(voidBedId))
                {
                    await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, voidBedId, "Cleaning", cancellationToken: cancellationToken);
                    await _eventPublisher.PublishWaitingListUpdatedAsync(bangsalId, cancellationToken);
                }

                foreach (var extraBangsalId in affectedBangsalIds)
                {
                    if (!string.IsNullOrWhiteSpace(extraBangsalId) &&
                        !string.Equals(extraBangsalId, bangsalId?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        await _eventPublisher.PublishBedStatusChangedAsync(extraBangsalId, saved.Bed.BedId, "Cleaning", cancellationToken: cancellationToken);
                        await _eventPublisher.PublishWaitingListUpdatedAsync(extraBangsalId, cancellationToken);
                    }
                }
            }

            // 8. Audit Log
            var audit = CreateAudit(saved, snapshotJson, request);
            _auditRepo.SaveChanges(audit);
        }

        private async Task HandleCancelBabyScopeAsync(PakaiBedModel saved, PakaiBedVoidCommand request, CancellationToken cancellationToken)
        {
            var occurredAt = _tglJamProvider.Now;
            var affectedBangsalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Resolve baby RegIds
            var babyRegIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_pakaiBedDal != null)
            {
                var dalBabies = await _pakaiBedDal.GetRoomingInBabyRegIdsAsync(request.PakaiBedId);
                if (dalBabies != null)
                {
                    foreach (var bReg in dalBabies.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        babyRegIds.Add(bReg.Trim());
                    }
                }
            }
            else if (_pakaiBedRepo != null)
            {
                var repoBabies = await _pakaiBedRepo.GetRoomingInBabyRegIdsAsync(request.PakaiBedId);
                if (repoBabies != null)
                {
                    foreach (var bReg in repoBabies.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        babyRegIds.Add(bReg.Trim());
                    }
                }
            }

            if (babyRegIds.Count == 0 && saved.Tarif == 0)
            {
                babyRegIds.Add(saved.Reg.RegId);
            }

            // 2. Zero-Transaction Invariant Guard: check non-voided tindakan
            if (_tindakanRepo != null)
            {
                foreach (var babyRegId in babyRegIds)
                {
                    var existingTindakan = _tindakanRepo.ListData(RegModel.Key(babyRegId));
                    if (existingTindakan != null && existingTindakan.Any())
                    {
                        throw new InvalidOperationException("Tindakan bayi harus dibatalkan/void terlebih dahulu sebelum membatalkan penempatan.");
                    }
                }
            }

            // 3. Billing Lock Check for baby
            if (_billingLockChecker != null)
            {
                foreach (var babyRegId in babyRegIds)
                {
                    if (_billingLockChecker.CheckIsBillingLocked(babyRegId))
                    {
                        throw new BillingLockedException(babyRegId);
                    }
                }
            }

            // 4. Update domain model state if saved is the baby itself
            if (saved.Tarif == 0)
            {
                saved.Void(request.UserId, occurredAt);
                _pakaiBedRepo.SaveChanges(saved);
            }

            // 5. Void in DAL (only voids baby records and cleans up rooming in link)
            if (_pakaiBedDal != null)
            {
                var dalBangsals = await _pakaiBedDal.VoidRoomingInBabyAsync(request.PakaiBedId, request.UserId, request.VoidReason, occurredAt);
                if (dalBangsals != null)
                {
                    foreach (var b in dalBangsals.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        affectedBangsalIds.Add(b.Trim());
                    }
                }
            }

            // 6. Revert Waiting List for baby
            if (_waitingListRepo != null)
            {
                BangsalReff? targetBangsal = null;
                if (_bedRepo != null && _bangsalRepo != null && !string.IsNullOrWhiteSpace(saved.Bed?.BedId))
                {
                    var maybeBed = _bedRepo.LoadEntity(BedType.Key(saved.Bed.BedId));
                    if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
                    {
                        var maybeBsl = _bangsalRepo.LoadEntity(BangsalType.Key(maybeBed.Value.Bangsal.BangsalId));
                        if (maybeBsl.HasValue)
                        {
                            targetBangsal = maybeBsl.Value.ToReff();
                        }
                    }
                }
                targetBangsal ??= new BangsalReff(saved.Layanan.LayananId, saved.Layanan.LayananName);

                foreach (var babyRegId in babyRegIds)
                {
                    var babyWlList = _waitingListRepo.ListByRegId(babyRegId)?.ToList() ?? [];
                    var activeBabyWl = babyWlList.FirstOrDefault(x => x.WaitingListStatus == WaitingListStatusEnum.Waiting);

                    if (activeBabyWl != null)
                    {
                        if (!string.IsNullOrWhiteSpace(activeBabyWl.Bangsal?.BangsalId))
                        {
                            affectedBangsalIds.Add(activeBabyWl.Bangsal.BangsalId.Trim());
                        }
                    }
                    else
                    {
                        var placedBabyWl = babyWlList.FirstOrDefault(x => x.WaitingListStatus == WaitingListStatusEnum.Placed);
                        if (placedBabyWl != null)
                        {
                            var revertedBabyWl = placedBabyWl.RevertToWaiting(request.UserId, occurredAt);
                            _waitingListRepo.SaveChanges(revertedBabyWl);
                            if (!string.IsNullOrWhiteSpace(revertedBabyWl.Bangsal?.BangsalId))
                            {
                                affectedBangsalIds.Add(revertedBabyWl.Bangsal.BangsalId.Trim());
                            }
                        }
                        else if (!babyWlList.Any(x => x.WaitingListStatus == WaitingListStatusEnum.Waiting || x.WaitingListStatus == WaitingListStatusEnum.Reserved))
                        {
                            if (_regRepo != null)
                            {
                                var maybeBabyReg = _regRepo.LoadEntity(RegModel.Key(babyRegId));
                                if (maybeBabyReg.HasValue && maybeBabyReg.Value.IsAktif)
                                {
                                    var babyReg = maybeBabyReg.Value;
                                    var babyKelas = babyReg.Kelas != null && babyReg.Kelas.KelasId != "-"
                                        ? babyReg.Kelas
                                        : (saved.Kelas != null && saved.Kelas.KelasId != "-" ? saved.Kelas : new KelasReff("-", "-"));
                                    var babyDokter = babyReg.Dokter != null
                                        ? new DokterReff(babyReg.Dokter.PpaId, babyReg.Dokter.PpaName)
                                        : new DokterReff("-", "-");
                                    var babyLayanan = saved.Layanan != null
                                        ? new LayananReff(saved.Layanan.LayananId, saved.Layanan.LayananName)
                                        : new LayananReff("-", "-");

                                    var fallbackBabyWl = WaitingListModel.Create(
                                        babyReg.RegId,
                                        babyReg.Pasien,
                                        babyKelas,
                                        targetBangsal,
                                        ArrivalSourceTypeEnum.Adm,
                                        babyLayanan,
                                        babyDokter,
                                        ClinicalUrgencyEnum.Routine,
                                        "Batal Check-in Rawat Gabung",
                                        priority: 0,
                                        auditUserId: request.UserId,
                                        createdAt: occurredAt);

                                    _waitingListRepo.Save(fallbackBabyWl);
                                    if (!string.IsNullOrWhiteSpace(fallbackBabyWl.Bangsal?.BangsalId))
                                    {
                                        affectedBangsalIds.Add(fallbackBabyWl.Bangsal.BangsalId.Trim());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 7. EMR Gateway Discharge
            if (_emr20Gateway != null)
            {
                foreach (var babyRegId in babyRegIds)
                {
                    await _emr20Gateway.DischargeRegRanapAsync(babyRegId, isGabung: true, cancellationToken);
                }
            }

            // 8. SignalR event notifications
            if (_eventPublisher != null)
            {
                var bangsalId = string.Empty;
                if (_bedRepo != null && !string.IsNullOrWhiteSpace(saved.Bed?.BedId))
                {
                    var maybeBed = _bedRepo.LoadEntity(BedType.Key(saved.Bed.BedId));
                    if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
                    {
                        bangsalId = maybeBed.Value.Bangsal.BangsalId;
                    }
                }
                if (string.IsNullOrWhiteSpace(bangsalId))
                {
                    bangsalId = saved.Layanan?.LayananId ?? string.Empty;
                }

                var voidBedId = (saved.Bed?.BedId ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(bangsalId) && !string.IsNullOrWhiteSpace(voidBedId))
                {
                    // Mother is still in the bed, so status remains Occupied
                    await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, voidBedId, "Occupied", cancellationToken: cancellationToken);
                    await _eventPublisher.PublishWaitingListUpdatedAsync(bangsalId, cancellationToken);
                }

                foreach (var extraBangsalId in affectedBangsalIds)
                {
                    if (!string.IsNullOrWhiteSpace(extraBangsalId) &&
                        !string.Equals(extraBangsalId, bangsalId?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        await _eventPublisher.PublishWaitingListUpdatedAsync(extraBangsalId, cancellationToken);
                    }
                }
            }

            // 9. Audit Log
            var snapshotJson = AuditLogSnapshotJson.Serialize(saved);
            var auditInfo = new AuditInfoType(request.UserId, occurredAt);
            var audit = CreateAudit(saved, snapshotJson, request, customReason: $"[Batal Rawat Gabung Bayi] {request.VoidReason}", customAuditInfo: auditInfo);
            _auditRepo.SaveChanges(audit);
        }

        private AuditLog CreateAudit(PakaiBedModel pkb, string snapShotJson, PakaiBedVoidCommand cmd, string? customReason = null, AuditInfoType? customAuditInfo = null)
        {
            var auditInfo = customAuditInfo ?? pkb.AuditTrail.Voided;
            var reason = customReason ?? cmd.VoidReason;
            var result = AuditLog.Create(
                auditInfo,
                actionType: "VOID",
                entityName: nameof(PakaiBedModel),
                entityId: pkb.PakaiBedId,
                reason: reason,
                originalDataJson: snapShotJson,
                correlationId: pkb.Reg.RegId,
                clientIpAddress: cmd.RemoteIpAddress,
                userAgent: cmd.UserAgent
                );
            return result;
        }
    }
}
