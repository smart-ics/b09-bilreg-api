using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases
{
    public record PakaiBedCreateCommand(
        string RegId,
        string LayananId,
        string TipeKamarId,
        string BedId,
        string UserId,
        DateTime TglJamIn,
        string BillingKelasId = "",
        string WaitingListId = "",
        string MotherPakaiBedId = "",
        bool IsRoomingIn = false,
        bool IsTitipKelas = false,
        bool IsNaikKelas = false) 
        : IRequest<PakaiBedCreateResult>;

    public record PakaiBedCreateResult(
        string PakaiBedId);

    public class PakaiBedCreateCommandHandler : IRequestHandler<PakaiBedCreateCommand, PakaiBedCreateResult>
    {
        private readonly IPakaiBedAlokasiRepo _pakaiBedAlokasiRepo;
        private readonly IBedOperationalRepo _bedOperationalRepo;
        private readonly IPakaiBedActiveOccupancyDal _activeOccupancyDal;
        private readonly IRegRepo _regRepo;
        private readonly IBedRepo _bedRepo;
        private readonly ILayananRepo _layananRepo;
        private readonly IKamarRepo _kamarRepo;
        private readonly ITipeKamarRepo _tipeKamarRepo;
        private readonly IRoomRateRepo _roomRateRepo;
        private readonly IKelasRepo _kelasRepo;

        public PakaiBedCreateCommandHandler(
            IPakaiBedAlokasiRepo pakaiBedAlokasiRepo,
            IBedOperationalRepo bedOperationalRepo,
            IPakaiBedActiveOccupancyDal activeOccupancyDal,
            IRegRepo regRepo,
            IBedRepo bedRepo,
            ILayananRepo layananRepo,
            IKamarRepo kamarRepo,
            ITipeKamarRepo tipeKamarRepo,
            IRoomRateRepo roomRateRepo, 
            IKelasRepo kelasRepo)
        {
            _pakaiBedAlokasiRepo = pakaiBedAlokasiRepo;
            _bedOperationalRepo = bedOperationalRepo;
            _activeOccupancyDal = activeOccupancyDal;
            _regRepo = regRepo;
            _bedRepo = bedRepo;
            _layananRepo = layananRepo;
            _kamarRepo = kamarRepo;
            _tipeKamarRepo = tipeKamarRepo;
            _roomRateRepo = roomRateRepo;
            _kelasRepo = kelasRepo;
        }

        public Task<PakaiBedCreateResult> Handle(PakaiBedCreateCommand request, CancellationToken cancellationToken)
        {
            // 1. Validasi Eksklusivitas Bed & Rooming-in Guard
            var activeOccupancy = _activeOccupancyDal.GetActiveByBed(request.BedId);
            if (request.IsRoomingIn)
            {
                if (string.IsNullOrWhiteSpace(request.MotherPakaiBedId))
                    throw new InvalidOperationException("Rooming-in memerlukan MotherPakaiBedId.");

                if (activeOccupancy is null)
                    throw new InvalidOperationException(
                        $"Tempat tidur {request.BedId} tidak sedang aktif ditempati alokasi ibu untuk Rooming-in.");

                if (activeOccupancy.PakaiBedId != request.MotherPakaiBedId)
                    throw new InvalidOperationException(
                        $"Tempat tidur {request.BedId} sedang ditempati alokasi {activeOccupancy.PakaiBedId}, bukan alokasi ibu {request.MotherPakaiBedId}.");
            }
            else
            {
                if (activeOccupancy is not null)
                    throw new InvalidOperationException(
                        $"Tempat tidur {request.BedId} sedang aktif ditempati pasien lain ({activeOccupancy.PakaiBedId}).");
            }

            // 2. Load Master Entities
            var reg = _regRepo.LoadEntity(RegModel.Key(request.RegId))
                .GetValueOrThrow("Reg not found");

            var bed = _bedRepo.LoadEntity(BedType.Key(request.BedId))
                .GetValueOrThrow("Bed not found");

            var kamar = _kamarRepo.LoadEntity(KamarType.Key(bed.Kamar.KamarId))
                .GetValueOrThrow("Kamar not found");

            var layanan = _layananRepo.LoadEntity(LayananType.Key(request.LayananId))
                .GetValueOrThrow("Layanan not found");

            var tipeKamar = _tipeKamarRepo.LoadEntity(TipeKamarType.Key(request.TipeKamarId))
                .GetValueOrThrow("Tipe Kamar not found");

            var physicalKelas = _kelasRepo.LoadEntity(KelasType.Key(kamar.Kelas.KelasId))
                .GetValueOrThrow("Kelas not found");

            // 3. Bed Readiness Guard
            var bedOp = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(request.BedId))
                .GetValueOrThrow($"Bed Operational {request.BedId} not found");

            var masterSnapshot = new BedMasterSnapshotType(
                bedId: bed.BedId,
                bangsalId: bed.Bangsal.BangsalId,
                kamarId: bed.Kamar.KamarId,
                isActive: bed.IsAktif);

            bedOp.AssertReadyForAllocation(masterSnapshot);

            // 4. Resolve Kelas & Tarif (Dual-Class & Rooming-in)
            var physicalKelasId = kamar.Kelas.KelasId;
            var billingKelasId = !string.IsNullOrWhiteSpace(request.BillingKelasId)
                ? request.BillingKelasId
                : physicalKelasId;

            var billingKelas = billingKelasId == physicalKelasId
                ? physicalKelas
                : _kelasRepo.LoadEntity(KelasType.Key(billingKelasId))
                    .GetValueOrThrow($"Billing Kelas {billingKelasId} not found");

            decimal tarif = 0;
            if (!request.IsRoomingIn)
            {
                var roomRate = _roomRateRepo.LoadEntity(kamar)
                    .GetValueOrThrow("Room Rate not found");

                var rate = roomRate.ListTipe
                    .FirstOrDefault(x => x.TipeKamar.TipeKamarId == request.TipeKamarId)
                    ?? throw new InvalidOperationException("Room Rate for Tipe Kamar not found");

                tarif = rate.TotalNilai;
            }

            var isTitipKelas = request.IsTitipKelas || (!request.IsNaikKelas && !string.IsNullOrWhiteSpace(request.BillingKelasId) && request.BillingKelasId != physicalKelasId);
            var isNaikKelas = request.IsNaikKelas;

            // 5. Unified ID & Timestamp
            var tglJamInUtc = request.TglJamIn.Kind switch
            {
                DateTimeKind.Utc => request.TglJamIn,
                DateTimeKind.Local => request.TglJamIn.ToUniversalTime(),
                _ => DateTime.SpecifyKind(request.TglJamIn, DateTimeKind.Utc)
            };

            var recordedAtUtc = DateTime.UtcNow < tglJamInUtc ? tglJamInUtc : DateTime.UtcNow;

            var periode = new PeriodePakaiBedType(
                new AuditInfoType(request.UserId, tglJamInUtc),
                AuditInfoType.Default);

            // Legacy model uses NunaId.NewLegacy("BE", 'A')
            var legacyPakaiBed = PakaiBedModel.Create(
                reg,
                periode.Masuk,
                layanan,
                bed,
                tipeKamar,
                billingKelas,
                tarif);

            var pakaiBedId = legacyPakaiBed.PakaiBedId;

            // 6. Build Modern Aggregate (PakaiBedAlokasiModel)
            var purpose = request.IsRoomingIn ? PakaiBedPurposeEnum.RoomingIn : PakaiBedPurposeEnum.Clinical;
            var role = request.IsRoomingIn ? OccupantRoleEnum.Associated : OccupantRoleEnum.Primary;
            var waitingListId = request.WaitingListId ?? string.Empty;
            if (!request.IsRoomingIn && string.IsNullOrWhiteSpace(waitingListId))
            {
                waitingListId = "-";
            }

            var requestId = NunaId.New("REQ");
            var evidenceId = !string.IsNullOrWhiteSpace(bedOp.LatestReadinessTransactionId)
                ? bedOp.LatestReadinessTransactionId
                : NunaId.New("EVD");

            var proposeTrans = new PakaiBedTransisiModel(
                NunaId.New("ATR"),
                pakaiBedId,
                PakaiBedTransisiEnum.Proposed,
                tglJamInUtc,
                recordedAtUtc,
                request.UserId,
                "Check-in",
                requestId,
                waitingListId,
                string.Empty);

            var assignTrans = new PakaiBedTransisiModel(
                NunaId.New("ATR"),
                pakaiBedId,
                PakaiBedTransisiEnum.Assigned,
                tglJamInUtc,
                recordedAtUtc,
                request.UserId,
                "Check-in",
                requestId,
                waitingListId,
                evidenceId);

            var alokasi = new PakaiBedAlokasiModel(
                pakaiBedId,
                1,
                reg.RegId,
                reg.Pasien.PasienId,
                bed.Bangsal.BangsalId,
                bed.Kamar.KamarId,
                bed.BedId,
                waitingListId,
                requestId,
                purpose,
                role,
                PakaiBedStatusEnum.Active,
                tglJamInUtc,
                tglJamInUtc,
                request.UserId,
                evidenceId,
                [proposeTrans, assignTrans],
                [],
                physicalKelasId: physicalKelasId,
                billingKelasId: billingKelasId,
                tipeKamarId: request.TipeKamarId,
                tarif: tarif,
                motherPakaiBedId: request.MotherPakaiBedId ?? string.Empty,
                isTitipKelas: isTitipKelas,
                isNaikKelas: isNaikKelas);

            // 7. Atomic Dual-Write Persistence
            _pakaiBedAlokasiRepo.SaveChanges(alokasi, legacyPakaiBed);

            return Task.FromResult(new PakaiBedCreateResult(pakaiBedId));
        }
    }
}