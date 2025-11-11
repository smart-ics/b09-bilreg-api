using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekListByLayananQuery(string LayananId) : IRequest<IEnumerable<JadwalPraktekListByLayananResponse>>, ILayananKey;

public record JadwalPraktekListByLayananResponse(
    string DokterId, string DokterName, 
    string LayananId, string LayananName,
    IEnumerable<JadwalPraktekDokterByLynHariResponse> ListHari);

public record JadwalPraktekDokterByLynHariResponse(
    string JadwalPraktekId, string Hari, string JamMulai, string JamSelesai);

public class JadwalPrektekListByLayananHandler : IRequestHandler<JadwalPraktekListByLayananQuery, IEnumerable<JadwalPraktekListByLayananResponse>>
{
    private readonly IJadwalPraktekRepo _jadwalRepo;

    public JadwalPrektekListByLayananHandler(IJadwalPraktekRepo jadwalRepo)
    {
        _jadwalRepo = jadwalRepo;
    }

    public Task<IEnumerable<JadwalPraktekListByLayananResponse>> Handle(JadwalPraktekListByLayananQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));

        var lyn = LayananType.Key(request.LayananId);
        var listJadwal = _jadwalRepo.ListData(lyn)?.ToList() ?? [];

        var result = listJadwal
            .GroupBy(x => new { x.Dokter.PetugasMedisId, x.Dokter.PetugasMedisName,
                x.Layanan.LayananId, x.Layanan.LayananName
            })
            .Select(g => new JadwalPraktekListByLayananResponse(
                g.Key.PetugasMedisId,
                g.Key.PetugasMedisName,
                g.Key.LayananId,
                g.Key.LayananName,
                g.OrderBy(j => j.Hari).ThenBy(j => j.JamMulai)
                 .Select(j => new JadwalPraktekDokterByLynHariResponse(
                     j.JadwalPraktekId,
                     j.Hari.ToString(),
                     j.JamMulai.ToString("HH:mm"),
                     j.JamSelesai.ToString("HH:mm")
                 ))
            ));

        return Task.FromResult( result );
    }
}
