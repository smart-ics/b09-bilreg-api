using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekListQuery(string DokterId) 
    : IRequest<IEnumerable<JadwalPraktekListResponse>>;

public record JadwalPraktekListResponse(
    string JadwalId,
    string DokterId,
    string DokterName,
    string Hari,
    string JamMulai,
    string JamSelesai);

public class JadwalPraktekListHandler : IRequestHandler<JadwalPraktekListQuery, IEnumerable<JadwalPraktekListResponse>>
{
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;

    public JadwalPraktekListHandler(IJadwalPraktekRepo jadwalPraktekRepo)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
    }

    public Task<IEnumerable<JadwalPraktekListResponse>> Handle(JadwalPraktekListQuery request, CancellationToken cancellationToken)
    {
        var dokter = PetugasMedisType.Key(request.DokterId);
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var response = listJadwal.Select(x => new JadwalPraktekListResponse(
            x.JadwalPraktekId, x.Dokter.PetugasMedisId, x.Dokter.PetugasMedisName, 
            x.Hari.ToString(), x.JamMulai.ToString("HH:mm"), x.JamSelesai.ToString("HH:mm")));
        return Task.FromResult(response);
    }
}
