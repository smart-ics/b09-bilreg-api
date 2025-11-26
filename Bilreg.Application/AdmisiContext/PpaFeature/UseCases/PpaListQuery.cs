using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListQuery(string SatuanTugasId) : IRequest<IEnumerable<PpaListResponse>>;

public record PpaListResponse(
    string PpaId,
    string PpaName,
    string NamaSingkat,
    string SmfId,
    string SmfName
);

public class PpaListHandler : IRequestHandler<PpaListQuery, IEnumerable<PpaListResponse>>
{
    private readonly IPpaRepo _ppaRepo;

    public PpaListHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<PpaListResponse>> Handle(PpaListQuery request,
        CancellationToken cancellationToken)
    {
        // var listPtgMed = _ppaRepo
        //     .ListData(SatTugasType.Key(request.SatuanTugasId))?
        //     .ToList() ?? [];
        //
        //
        // var response = listPtgMed.Select(x => new PpaListResponse(
        //     x.PetugasMedisId, x.PetugasMedisName, x.NamaSingkat, x.Smf.SmfId, x.Smf.SmfName));
        // return Task.FromResult(response);
        throw new NotImplementedException();
    }
}