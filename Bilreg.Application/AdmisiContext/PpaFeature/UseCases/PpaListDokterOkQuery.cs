using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListDokterOkQuery : IRequest<IEnumerable<PpaListDokterOkResponse>>;

public record PpaListDokterOkResponse(string DokterId, string DokterName);

public class PpaListDokterOkQueryHandler : IRequestHandler<PpaListDokterOkQuery, IEnumerable<PpaListDokterOkResponse>>
{
    private readonly IPpaRepo _ppaRepo;
    private readonly ILayananRepo _layananRepo;

    public PpaListDokterOkQueryHandler(IPpaRepo ppaRepo, 
        ILayananRepo layananRepo)
    {
        _ppaRepo = ppaRepo;
        _layananRepo = layananRepo;
    }

    public Task<IEnumerable<PpaListDokterOkResponse>> Handle(PpaListDokterOkQuery request, CancellationToken cancellationToken)
    {
        var listLayanan = _layananRepo
            .ListData()
            .Where(x => x.GroupSpesialis == GroupSpesialisType.Bedah || 
                        x.GroupSpesialis == GroupSpesialisType.Obgyn)
            .ToList() ?? [];

        var listDokter = _ppaRepo.ListData(ProfesiType.Dokter, listLayanan);
        var result = listDokter.Select(x => new PpaListDokterOkResponse(x.PpaId, x.PpaName));
        return Task.FromResult(result);
    }
}
