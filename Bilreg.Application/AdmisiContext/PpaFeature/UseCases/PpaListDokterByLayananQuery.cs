using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListDokterByLayananQuery(string LayananId) 
    : IRequest<IEnumerable<PpaListDokterByLayananResponse>>, ILayananKey ;
public record PpaListDokterByLayananResponse(
    string DokterId, string DokterName);
public class PpaListDokterByLayananHandler 
    : IRequestHandler<PpaListDokterByLayananQuery, IEnumerable<PpaListDokterByLayananResponse>>
{
    private readonly IPpaRepo _ppaRepo;
    public PpaListDokterByLayananHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }
    public Task<IEnumerable<PpaListDokterByLayananResponse>> Handle(PpaListDokterByLayananQuery request, 
        CancellationToken cancellationToken)
    {
        
        var listPpaDokter = _ppaRepo
            .ListData(ProfesiType.Dokter, [LayananType.Key(request.LayananId)])?
            .ToList() ?? [];

        var result = listPpaDokter
            .Select(x => new PpaListDokterByLayananResponse(x.PpaId, x.PpaName))
            .Distinct();
        return Task.FromResult(result);
    }
}
