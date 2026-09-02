using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases
{
    public record PakaiBedListByRegQuery(string RegId) : IRequest<IEnumerable<PakaiBedListResponse>>, IRegKey;
    public record PakaiBedListResponse(
        string PakaiBedId,
        RegReff Reg,
        LayananReff Layanan,
        BedReff Bed,
        KamarReff Kamar,
        TipeKamarReff TipeKamar,
        decimal Tarif,
        PeriodePakaiBedType Periode);

    public class PakaiBedListByRegQueryHandler : IRequestHandler<PakaiBedListByRegQuery, IEnumerable<PakaiBedListResponse>>
    {
        private readonly IPakaiBedRepo _pakaiBedRepo;
        private readonly IBedRepo _bedRepo;
        private readonly IKamarRepo _kamarRepo;
        public PakaiBedListByRegQueryHandler(IPakaiBedRepo pakaiBedRepo, IBedRepo bedRepo, IKamarRepo kamarRepo)
        {
            _pakaiBedRepo = pakaiBedRepo;
            _bedRepo = bedRepo;
            _kamarRepo = kamarRepo;
        }
        public async Task<IEnumerable<PakaiBedListResponse>> Handle(PakaiBedListByRegQuery request, CancellationToken cancellationToken)
        {
            var pakaiBeds = _pakaiBedRepo.ListData(request).ToList();
            var responses = new List<PakaiBedListResponse>();
            foreach (var pakaiBed in pakaiBeds)
            {
                var bed = _bedRepo.LoadEntity(BedType.Key(pakaiBed.Bed.BedId))
                .GetValueOrThrow("Bed not found");
            var kamar = _kamarRepo.LoadEntity(KamarType.Key(bed.Kamar.KamarId))
                .GetValueOrThrow("Kamar not found");

            var response = new PakaiBedListResponse(
                pakaiBed.PakaiBedId,
                pakaiBed.Reg,
                pakaiBed.Layanan,
                pakaiBed.Bed,
                kamar.ToReff(),
                pakaiBed.TipeKamar,
                pakaiBed.Tarif,
                pakaiBed.Periode
            );
            responses.Add(response);
        }
        return responses;
        }
    }
}
