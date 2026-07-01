using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public record PolisGetByNoPesertaQuery(string NoPeserta) : IRequest<PolisGetByNoPesertaRespone>;

public record PolisGetByNoPesertaRespone(
    string PolisId,
    string NoPolis,
    string AtasNama,
    string ExpiredDate,
    TipeJaminanReff TipeJaminan,
    KelasReff Kelas,
    bool IsCoverRajal,
    IEnumerable<PolisCoverResponse> PolisCovers);

public record PolisCoverResponse(
    string PolisId,
    PasienReff Pasien,
    StatusPesertaType Status,
    string ExpiredDate);

public class PolisGetByNoPesertaHandler : IRequestHandler<PolisGetByNoPesertaQuery, PolisGetByNoPesertaRespone>
{
    private readonly IPolisRepo _polisRepo;

    public PolisGetByNoPesertaHandler(IPolisRepo polisRepo)
    {
        _polisRepo = polisRepo;
    }

    public Task<PolisGetByNoPesertaRespone> Handle(PolisGetByNoPesertaQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.NoPeserta, nameof(request.NoPeserta));

        var polis = _polisRepo.GetDataByNoPeserta(request.NoPeserta);
        if (polis.PolisId == "-") throw new KeyNotFoundException($"Polis dengan Nomor peserta {request.NoPeserta} not found");
        
        var listCover = polis.ListCover
            .Select(x => 
                new PolisCoverResponse(x.PolisId, x.Pasien, x.Status, x.ExpiredDate.ToString("yyyy-MM-dd")));
        var result = new PolisGetByNoPesertaRespone(
            polis.PolisId, polis.NoPolis, polis.AtasNama, polis.ExpiredDate.ToString("yyyy-MM-dd"),
            polis.TipeJaminan, polis.Kelas, polis.IsCoverRajal, listCover);

        return Task.FromResult(result);
    }
}
