using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases
{
    public record PakaiBedGetQuery(string PakaiBedId) : IRequest<PakaiBedGetResponse>, IPakaiBed;
    public record PakaiBedGetResponse(
        string PakaiBedId,
        RegReff Reg,
        LayananReff Layanan,
        BedReff Bed,
        KamarReff Kamar,
        TipeKamarReff TipeKamar,
        decimal Tarif,
        PeriodePakaiBedType Periode);

    public class PakaiBedGetQueryHandler : IRequestHandler<PakaiBedGetQuery, PakaiBedGetResponse>
    {
        private readonly IPakaiBedRepo _pakaiBedRepo;
        private readonly IBedRepo _bedRepo;
        private readonly IKamarRepo _kamarRepo;
        public PakaiBedGetQueryHandler(IPakaiBedRepo pakaiBedRepo, IBedRepo bedRepo, IKamarRepo kamarRepo)
        {
            _pakaiBedRepo = pakaiBedRepo;
            _bedRepo = bedRepo;
            _kamarRepo = kamarRepo;
        }
        public async Task<PakaiBedGetResponse> Handle(PakaiBedGetQuery request, CancellationToken cancellationToken)
        {
            var pakaiBed = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId))
                .GetValueOrThrow("Pakai Bed not found");
            var bed = _bedRepo.LoadEntity(BedType.Key(pakaiBed.Bed.BedId))
                .GetValueOrThrow("Bed not found");
            var kamar = _kamarRepo.LoadEntity(KamarType.Key(bed.Kamar.KamarId))
                .GetValueOrThrow("Kamar not found");

            var response = new PakaiBedGetResponse(
                pakaiBed.PakaiBedId,
                pakaiBed.Reg,
                pakaiBed.Layanan,
                pakaiBed.Bed,
                kamar.ToReff(),
                pakaiBed.TipeKamar,
                pakaiBed.Tarif,
                pakaiBed.Periode
            );
            return response;
        }
    }
}
