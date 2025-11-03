using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPrektekListByLayananQuery(string LayananId) : IRequest<IEnumerable<JadwalPrektekListByLayananResponse>>, ILayananKey;

public record JadwalPrektekListByLayananResponse(
    string DokterId, string DokterName, IEnumerable<JadwalPraktekDokterByLynHariResponse> Hari);

public record JadwalPraktekDokterByLynHariResponse(
    string Hari, string JamMulai, string JamSelesai);

public class JadwalPrektekListByLayananHandler : IRequestHandler<JadwalPrektekListByLayananQuery, IEnumerable<JadwalPrektekListByLayananResponse>>
{
    private readonly IJadwalPraktekRepo _jadwalRepo;

    public JadwalPrektekListByLayananHandler(IJadwalPraktekRepo jadwalRepo)
    {
        _jadwalRepo = jadwalRepo;
    }

    public Task<IEnumerable<JadwalPrektekListByLayananResponse>> Handle(JadwalPrektekListByLayananQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));

        var lyn = LayananType.Key(request.LayananId);
        var listJadwal = _jadwalRepo.ListData(lyn)?.ToList() ?? [];

        var result = listJadwal
            .GroupBy(x => new { x.Dokter.PetugasMedisId, x.Dokter.PetugasMedisName })
            .Select(g => new JadwalPrektekListByLayananResponse(
                g.Key.PetugasMedisId,
                g.Key.PetugasMedisName,
                g.OrderBy(j => j.Hari)
                 .Select(j => new JadwalPraktekDokterByLynHariResponse(
                     j.Hari.ToString(),
                     j.JamMulai.ToString("HH:mm"),
                     j.JamSelesai.ToString("HH:mm")
                 ))
            ));

        return Task.FromResult( result );
    }
}
