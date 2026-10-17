using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
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

        public PakaiBedCreateCommandHandler(IPakaiBedRepo pakaiBedRepo,
            IPakaiBedAlokasiRepo pakaiBedAlokasiRepo,
            IRegRepo regRepo, IBedRepo bedRepo,
            ILayananRepo layananRepo, IKamarRepo kamarRepo,
            ITipeKamarRepo tipeKamarRepo, IRoomRateRepo roomRateRepo, 
            IKelasRepo kelasRepo,
            IWaitingListRepo waitingListRepo)
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
        }

        public Task<PakaiBedCreateResult> Handle(PakaiBedCreateCommand request, CancellationToken cancellationToken)
        {
            var reg = _regRepo.LoadEntity(RegModel.Key(request.RegId))
                .GetValueOrThrow("Reg not found");

            var bed = _bedRepo.LoadEntity(BedType.Key(request.BedId))
                .GetValueOrThrow("Bed not found");

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

            var pakaiBed = PakaiBedModel.Create(
                reg, 
                periode.Masuk, 
                layanan, 
                bed, 
                tipeKamar, 
                kelas,
                rate.TotalNilai);

            using var trans = TransHelper.NewScope();

            _pakaiBedRepo.SaveChanges(pakaiBed);

            if (waitingList is not null)
            {
                waitingList = waitingList.Place(bed.BedId, pakaiBed.PakaiBedId, request.UserId, request.TglJamIn);
                _waitingListRepo.SaveChanges(waitingList);
            }

            trans.Complete();

            return Task.FromResult(new PakaiBedCreateResult(pakaiBed.PakaiBedId));
        }
    }
}