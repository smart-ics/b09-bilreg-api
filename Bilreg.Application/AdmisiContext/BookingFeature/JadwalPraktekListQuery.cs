using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekListQuery(string DokterId) 
    : IRequest<IEnumerable<JadwalPraktekListResponse>>;

public record JadwalPraktekListResponse(
    string DokterId, string DokterName,
    string LayananId, string LayananName,
    IEnumerable<JadwalPraktekListHariResponse> ListHari);
public record JadwalPraktekListHariResponse(
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

        var result = listJadwal
            .GroupBy(x => new { x.Dokter.PetugasMedisId, x.Dokter.PetugasMedisName, 
                x.Layanan.LayananId, x.Layanan.LayananName })
            .Select(g => new JadwalPraktekListResponse(
                g.Key.PetugasMedisId,
                g.Key.PetugasMedisName,
                g.Key.LayananId,
                g.Key.LayananName,
                g.OrderBy(j => j.Hari).ThenBy(j => j.JamMulai)
                 .Select(j => new JadwalPraktekListHariResponse(
                     j.Hari.ToString(),
                     j.JamMulai.ToString("HH:mm"),
                     j.JamSelesai.ToString("HH:mm")
                 ))
            ));

        return Task.FromResult(result);
    }
}
