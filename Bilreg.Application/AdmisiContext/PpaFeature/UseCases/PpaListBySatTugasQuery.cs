using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListBySatTugasQuery(IEnumerable<string> listSatTugas) : IRequest<IEnumerable<PpaListBySatTugasResponse>>;

public record PpaListBySatTugasResponse(string PpaId, string PpaName);

public class PpaListSatTugasHandler : IRequestHandler<PpaListBySatTugasQuery, IEnumerable<PpaListBySatTugasResponse>>
{
    private readonly IPpaRepo _ppaRepo;

    public PpaListSatTugasHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<PpaListBySatTugasResponse>> Handle(PpaListBySatTugasQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request.listSatTugas, nameof(request.listSatTugas));
        if (request.listSatTugas.Count() <= 0)
            throw new ArgumentException("SatuanTugas tidak boleh kosong");
        
        var listSatTugas = request.listSatTugas.Select(x => new SatTugasType(x, "-", ProfesiType.Default));
        var listPpa = _ppaRepo.ListData(listSatTugas)?.ToList() ?? [];

        var result = listPpa.Select(x => new PpaListBySatTugasResponse(x.PpaId, x.PpaId))?.ToList() ?? [];
        
        return Task.FromResult(result.AsEnumerable());
    }
}
