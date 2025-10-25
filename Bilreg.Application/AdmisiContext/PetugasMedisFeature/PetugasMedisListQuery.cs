using Bilreg.Application.AdmisiContext.PetugasMedisSub;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisListQuery() : IRequest<IEnumerable<PetugasMedisListResponse>>;

public record PetugasMedisListResponse(
    string PetugasMedisId,
    string PetugasMedisName,
    string NamaSingkat,
    string SmfId,
    string SmfName
);

public class PetugasMedisListHandler : IRequestHandler<PetugasMedisListQuery, IEnumerable<PetugasMedisListResponse>>
{
    private readonly IPetugasMedisRepo _petugasMedisRepo;

    public PetugasMedisListHandler(IPetugasMedisRepo petugasMedisRepo)
    {
        _petugasMedisRepo = petugasMedisRepo;
    }

    public Task<IEnumerable<PetugasMedisListResponse>> Handle(PetugasMedisListQuery request,
        CancellationToken cancellationToken)
    {
        var listPtgMed = _petugasMedisRepo.ListData()?.ToList() ?? [];
        var response = listPtgMed.Select(x => new PetugasMedisListResponse(
            x.PetugasMedisId, x.PetugasMedisName, x.NamaSingkat, x.Smf.SmfId, x.Smf.SmfName));
        return Task.FromResult(response);
    }
}