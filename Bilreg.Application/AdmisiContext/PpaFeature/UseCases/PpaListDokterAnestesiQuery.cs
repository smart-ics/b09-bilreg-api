using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListDokterAnestesiQuery : IRequest<IEnumerable<PpaListDokterAnestesiResponse>>;

public record PpaListDokterAnestesiResponse(string DokterId, string DokterName);

public class PpaListDokterAnestesiQueryHandler : IRequestHandler<PpaListDokterAnestesiQuery, IEnumerable<PpaListDokterAnestesiResponse>>
{
    private readonly IPpaRepo _repo;

    public PpaListDokterAnestesiQueryHandler(IPpaRepo repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<PpaListDokterAnestesiResponse>> Handle(PpaListDokterAnestesiQuery request, CancellationToken cancellationToken)
    {
        //  BUILD
        var listDokter = _repo.ListData(ProfesiType.Dokter);

        var listDokterAnestesi = listDokter?
            .Where(x => x.GroupSpesialis == GroupSpesialisType.Anestesi)
            ?? new List<PpaView>();

        var result = listDokterAnestesi
            .Select(x => new PpaListDokterAnestesiResponse(x.PpaId, x.PpaName));
        return Task.FromResult(result);
    }
}
