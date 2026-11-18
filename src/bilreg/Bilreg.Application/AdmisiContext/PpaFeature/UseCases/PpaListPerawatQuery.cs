using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListPerawatQuery()
    : IRequest<IEnumerable<PpaListPerawatResponse>>;

public record PpaListPerawatResponse(
    string PpaId,
    string PpaName);

public class PpaListPerawatHandler
    : IRequestHandler<PpaListPerawatQuery, IEnumerable<PpaListPerawatResponse>>
{
    private readonly IPpaRepo _ppaRepo;

    public PpaListPerawatHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<PpaListPerawatResponse>> Handle(
        PpaListPerawatQuery request,
        CancellationToken cancellationToken)
    {
        var listPpaPerawat = _ppaRepo
            .ListData(ProfesiType.Perawat)?
            .ToList() ?? [];

        var result = listPpaPerawat
            .Select(x => new PpaListPerawatResponse(x.PpaId, x.PpaName));

        return Task.FromResult(result);
    }
}
