using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaGetQuery(string PpaId) 
    : IRequest<PpaGetResponse>, IPpaKey;

public record PpaSatTugasGetResponse(string SatTugasId, 
    string SatTugasName, bool IsUtama);

public record PpaLayananGetResponse(
    string LayananId, string LayananName);

public record PpaGetResponse(
    string PpaId, string PpaName, string NamaSingkat,
    string SmfId, string SmfName, 
    string GroupSpesialisId, string GroupSpesialisName,
    IEnumerable<PpaSatTugasGetResponse> ListSatTugas,
    IEnumerable<PpaLayananGetResponse> ListLayanan);

public class PpaGetHandler : IRequestHandler<PpaGetQuery, PpaGetResponse>
{
    private readonly IPpaRepo _ppaRepo;

    public PpaGetHandler(IPpaRepo ppaRepo)
    {
        _ppaRepo = ppaRepo;
    }
    public Task<PpaGetResponse> Handle(PpaGetQuery request, CancellationToken cancellationToken)
    {
        var ptgMedis = _ppaRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"PetugasMedis {request.PpaId} not found"));

        // RESPONSE
        var response = new PpaGetResponse(
            ptgMedis.PpaId,
            ptgMedis.PpaName,
            ptgMedis.NamaSingkat,
            ptgMedis.Smf.SmfId,
            ptgMedis.Smf.SmfName,
            ptgMedis.GroupSpesialis.GroupSpesialisId,
            ptgMedis.GroupSpesialis.GroupSpesialisName,
            ptgMedis.ListSatTugas.Select(x => new PpaSatTugasGetResponse(
                x.SatTugas.SatTugasId,
                x.SatTugas.SatTugasName,
                x.IsUtama)),
            ptgMedis.ListLayanan.Select(x => new PpaLayananGetResponse(
                x.Layanan.LayananId,
                x.Layanan.LayananName))
        );
        return Task.FromResult(response);
    }
}