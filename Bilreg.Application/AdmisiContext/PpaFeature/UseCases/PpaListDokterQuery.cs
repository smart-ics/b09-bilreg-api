using Ardalis.GuardClauses;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListDokterQuery() 
    : IRequest<IEnumerable<PpaListDokterResponse>> ;
public record PpaListDokterResponse(
    string DokterId, string DokterName);

public class PpaListDokterHandler 
    : IRequestHandler<PpaListDokterQuery, IEnumerable<PpaListDokterResponse>>
{
    private readonly IPpaRepo _ppaRepo;
    public PpaListDokterHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }
    public Task<IEnumerable<PpaListDokterResponse>> Handle(PpaListDokterQuery request, 
        CancellationToken cancellationToken)
    {
        
        var listPpaDokter = _ppaRepo
            .ListData(ProfesiType.Dokter)?
            .ToList() ?? [];

        var result = listPpaDokter
            .Select(x => new PpaListDokterResponse(x.PpaId, x.PpaName));
        return Task.FromResult(result);
    }
}
