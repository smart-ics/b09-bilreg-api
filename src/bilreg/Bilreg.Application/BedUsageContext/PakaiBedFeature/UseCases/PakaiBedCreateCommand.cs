using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.Integration;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
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
        string? WaitingListId = null) 
        : IRequest<PakaiBedCreateResult>;

    public record PakaiBedCreateResult(
        string PakaiBedId);

    public class PakaiBedCreateCommandHandler : IRequestHandler<PakaiBedCreateCommand, PakaiBedCreateResult>
    {
        private readonly IRegRepo _regRepo;
        private readonly IBedRepo _bedRepo;
        private readonly IKamarRepo _kamarRepo;
        private readonly IKelasRepo _kelasRepo;
        private readonly ITipeKamarRepo _tipeKamarRepo;
        private readonly IRoomRateRepo _roomRateRepo;
        private readonly ILayananRepo _layananRepo;
        private readonly IPakaiBedRepo _pakaiBedRepo;
        private readonly IPakaiBedAlokasiRepo _pakaiBedAlokasiRepo;
        private readonly IWaitingListRepo _waitingListRepo;
        private readonly IBedOperationalRepo? _bedOperationalRepo;
        private readonly IEmr20AntrianRanapGateway? _emr20Gateway;

        public PakaiBedCreateCommandHandler(IPakaiBedRepo pakaiBedRepo,
            IPakaiBedAlokasiRepo pakaiBedAlokasiRepo,
            IRegRepo regRepo, IBedRepo bedRepo,
            ILayananRepo layananRepo, IKamarRepo kamarRepo,
            ITipeKamarRepo tipeKamarRepo, IRoomRateRepo roomRateRepo, 
            IKelasRepo kelasRepo,
            IWaitingListRepo waitingListRepo,
            IBedOperationalRepo? bedOperationalRepo = null,
            IEmr20AntrianRanapGateway? emr20Gateway = null)
        {
            _pakaiBedRepo = pakaiBedRepo;
            _pakaiBedAlokasiRepo = pakaiBedAlokasiRepo;
            _regRepo = regRepo;
            _bedRepo = bedRepo;
            _layananRepo = layananRepo;
            _kamarRepo = kamarRepo;
            _tipeKamarRepo = tipeKamarRepo;
            _roomRateRepo = roomRateRepo;
            _kelasRepo = kelasRepo;
            _waitingListRepo = waitingListRepo;
            _bedOperationalRepo = bedOperationalRepo;
            _emr20Gateway = emr20Gateway;
        }

        public async Task<PakaiBedCreateResult> Handle(PakaiBedCreateCommand request, CancellationToken cancellationToken)
        {
            var reg = _regRepo.LoadEntity(RegModel.Key(request.RegId))
                .GetValueOrThrow("Reg not found");

            var bed = _bedRepo.LoadEntity(BedType.Key(request.BedId))
                .GetValueOrThrow("Bed not found");

            if (_bedOperationalRepo != null)
            {
                var maybeBedOp = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(request.BedId));
                if (maybeBedOp.HasValue)
                {
                    var bedOp = maybeBedOp.Value;
                    if (bedOp.CurrentReadiness != BedReadinessStatusEnum.Ready)
                    {
                        if (bedOp.CurrentReadiness == BedReadinessStatusEnum.CleaningRequired)
                        {
                            throw new InvalidOperationException(
                                $"Tempat tidur '{request.BedId}' masih dalam proses pembersihan/Housekeeping. Selesaikan pembersihan terlebih dahulu sebelum digunakan.");
                        }

                        throw new InvalidOperationException(
                            $"Tempat tidur '{request.BedId}' tidak siap pakai (status kesiapan: {bedOp.CurrentReadiness?.ToString() ?? "None"}). Selesaikan pembersihan terlebih dahulu sebelum digunakan.");
                    }

                    if (bedOp.HasUnresolvedBlocker)
                    {
                        throw new InvalidOperationException(
                            $"Tempat tidur '{request.BedId}' memiliki kendala/blocker aktif: {bedOp.CurrentBlockerState.Reason}.");
                    }
                }
            }

            var kamar = _kamarRepo.LoadEntity(KamarType.Key(bed.Kamar.KamarId))
                .GetValueOrThrow("Kamar not found");

            var layanan = _layananRepo.LoadEntity(LayananType.Key(request.LayananId))
                .GetValueOrThrow("Layanan not found");

            var roomRate = _roomRateRepo.LoadEntity(kamar)
                .GetValueOrThrow("Room Rate not found");

            var tipeKamar = _tipeKamarRepo.LoadEntity(TipeKamarType.Key(request.TipeKamarId))
                .GetValueOrThrow("Tipe Kamar not found");

            var kelas = _kelasRepo.LoadEntity(KelasType.Key(kamar.Kelas.KelasId))
                .GetValueOrThrow("Kelas not found");

            var rate = roomRate.ListTipe
                .Where(x => x.TipeKamar.TipeKamarId == request.TipeKamarId)
                .FirstOrDefault()
                ?? throw new Exception("Room Rate for Tipe Kamar not found");

            var listKomponen = new List<PakaiBedKomponenType>();

            if (roomRate is RoomRateDailyType dailyRate)
            {
                var dailyTipes = dailyRate.ListTipe
                    .Where(x => x.TipeKamar.TipeKamarId == request.TipeKamarId)
                    .ToList();
                foreach (var d in dailyTipes)
                {
                    listKomponen.AddRange(d.ListKomponen.Select(x =>
                        new PakaiBedKomponenType(x.Komponen.KomponenId, x.Komponen.KomponenName, x.Nilai, d.HariKe)));
                }
            }
            else if (rate is RoomRateRegulerTipeType regTipe)
            {
                listKomponen.AddRange(regTipe.ListKomponen.Select(x =>
                    new PakaiBedKomponenType(x.Komponen.KomponenId, x.Komponen.KomponenName, x.Nilai, 1)));
            }
            else if (rate is RoomRateKelasType kelasTipe)
            {
                listKomponen.AddRange(kelasTipe.ListKomponen.Select(x =>
                    new PakaiBedKomponenType(x.Komponen.KomponenId, x.Komponen.KomponenName, x.Nilai, 1)));
            }
            else if (rate is RoomRateDayType dayTipe)
            {
                listKomponen.AddRange(dayTipe.ListKomponen.Select(x =>
                    new PakaiBedKomponenType(x.Komponen.KomponenId, x.Komponen.KomponenName, x.Nilai, dayTipe.HariKe)));
            }

            // Fallback jika tidak ada komponen tarif terdaftar namun ada total nilai
            if (listKomponen.Count == 0 && rate.TotalNilai > 0)
            {
                listKomponen.Add(new PakaiBedKomponenType("AKO", "Akomodasi", rate.TotalNilai, 1));
            }

            WaitingListModel? waitingList = null;
            if (!string.IsNullOrWhiteSpace(request.WaitingListId))
            {
                waitingList = _waitingListRepo.LoadEntity(WaitingListModel.Key(request.WaitingListId))
                    .Match(
                        onSome: wl => wl,
                        onNone: () => throw new KeyNotFoundException($"Waiting List with ID '{request.WaitingListId}' not found."));

                if (!waitingList.IsActive)
                    throw new InvalidOperationException($"Waiting List '{request.WaitingListId}' is not active (current status: {waitingList.WaitingListStatus}).");
            }

            var periode = new PeriodePakaiBedType(
                new AuditInfoType(request.UserId, request.TglJamIn),
                AuditInfoType.Default
                );

            var layananDkId = reg.JenisInap?.JenisInapId == "3"
                ? "88"
                : (layanan.LayananDk?.LayananDkId ?? string.Empty);

            var pakaiBed = PakaiBedModel.Create(
                reg, 
                periode.Masuk, 
                layanan, 
                bed, 
                tipeKamar, 
                kelas,
                rate.TotalNilai,
                listKomponen,
                kelas.KelasId,
                layananDkId);

            using var trans = TransHelper.NewScope();

            _pakaiBedRepo.SaveChanges(pakaiBed);

            if (waitingList is not null)
            {
                waitingList = waitingList.Place(bed.BedId, pakaiBed.PakaiBedId, request.UserId, request.TglJamIn);
                _waitingListRepo.SaveChanges(waitingList);
            }

            if (_bedOperationalRepo != null)
            {
                var maybeBedOp = _bedOperationalRepo.LoadEntity(BedOperationalModel.Key(request.BedId));
                if (maybeBedOp.HasValue)
                {
                    var bedOp = maybeBedOp.Value.AdvanceOccupancyEpoch();
                    _bedOperationalRepo.SaveChanges(bedOp);
                }
            }

            trans.Complete();

            if (_emr20Gateway != null)
            {
                await _emr20Gateway.AddRegRanapAsync(request.RegId, cancellationToken);
            }

            return new PakaiBedCreateResult(pakaiBed.PakaiBedId);
        }
    }
}
