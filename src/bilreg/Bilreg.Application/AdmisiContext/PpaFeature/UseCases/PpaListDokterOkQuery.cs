using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListDokterOkQuery : IRequest<IEnumerable<PpaListDokterOkResponse>>;

public record PpaListDokterOkResponse(string DokterId, string DokterName);

public class PpaListDokterOkQueryHandler : IRequestHandler<PpaListDokterOkQuery, IEnumerable<PpaListDokterOkResponse>>
{
    private readonly IPpaRepo _ppaRepo;
    
    public PpaListDokterOkQueryHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<PpaListDokterOkResponse>> Handle(PpaListDokterOkQuery request, CancellationToken cancellationToken)
    {
        
        var listDokter = _ppaRepo.ListData(ProfesiType.Dokter)?.ToList() ?? [];
        var listDokterOk = listDokter
            .Where(x => x.GroupSpesialis == GroupSpesialisType.Bedah ||
                        x.GroupSpesialis == GroupSpesialisType.Obgyn)
            .ToList() ?? [];
        var result = listDokterOk
            .Select(x => new PpaListDokterOkResponse(x.PpaId, x.PpaName))
            .Distinct()
            .ToList();
        return Task.FromResult(result.AsEnumerable());
    }
}
