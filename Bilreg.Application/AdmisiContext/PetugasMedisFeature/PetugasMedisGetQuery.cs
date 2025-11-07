using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature;

public record PetugasMedisGetQuery(string PetugasMedisId) 
    : IRequest<PetugasMedisGetResponse>, IPetugasMedisKey;

public record PetugasMedisSatTugasGetResponse(string SatTugasId, 
    string SatTugasName, bool IsUtama);

public record PetugasMedisLayananGetResponse(
    string LayananId, string LayananName);

public record PetugasMedisGetResponse(
    string PetugasMedisId, string PetugasMedisName, string NamaSingkat,
    string SmfId, string SmfName,
    IEnumerable<PetugasMedisSatTugasGetResponse> ListSatTugas,
    IEnumerable<PetugasMedisLayananGetResponse> ListLayanan);

public class PetugasMedisGetHandler : IRequestHandler<PetugasMedisGetQuery, PetugasMedisGetResponse>
{
    private readonly IPetugasMedisRepo _petugasMedisRepo;

    public PetugasMedisGetHandler(IPetugasMedisRepo petugasMedisRepo)
    {
        _petugasMedisRepo = petugasMedisRepo;
    }
    public Task<PetugasMedisGetResponse> Handle(PetugasMedisGetQuery request, CancellationToken cancellationToken)
    {
        var ptgMedis = _petugasMedisRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"PetugasMedis {request.PetugasMedisId} not found"));

        // RESPONSE
        var response = new PetugasMedisGetResponse(
            ptgMedis.PetugasMedisId,
            ptgMedis.PetugasMedisName,
            ptgMedis.NamaSingkat,
            ptgMedis.Smf.SmfId,
            ptgMedis.Smf.SmfName,
            ptgMedis.ListSatTugas.Select(x => new PetugasMedisSatTugasGetResponse(
                x.SatTugas.SatTugasId,
                x.SatTugas.SatTugasName,
                x.IsUtama)),
            ptgMedis.ListLayanan.Select(x => new PetugasMedisLayananGetResponse(
                x.Layanan.LayananId,
                x.Layanan.LayananName))
        );
        return Task.FromResult(response);
    }
}