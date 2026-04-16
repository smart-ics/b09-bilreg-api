using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekListQuery(string DokterId) 
    : IRequest<IEnumerable<JadwalPraktekListResponse>>;

public record JadwalPraktekListResponse(
    string DokterId, string DokterName,
    string LayananId, string LayananName,
    string RuangId, string RuangName,
    IEnumerable<JadwalPraktekListHariResponse> ListHari);
public record JadwalPraktekListHariResponse(
    string JadwalPraktekId,
    string Hari,
    string JamMulai,
    string JamSelesai,
    int MaxPasien);

public class JadwalPraktekListHandler : IRequestHandler<JadwalPraktekListQuery, IEnumerable<JadwalPraktekListResponse>>
{
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;

    public JadwalPraktekListHandler(IJadwalPraktekRepo jadwalPraktekRepo)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
    }

    public Task<IEnumerable<JadwalPraktekListResponse>> Handle(JadwalPraktekListQuery request, CancellationToken cancellationToken)
    {
        var dokter = PpaType.Key(request.DokterId);
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];

        var result = listJadwal
            .GroupBy(x => new {
                PetugasMedisId = x.Dokter.PpaId,
                PetugasMedisName = x.Dokter.PpaName, 
                x.Layanan.LayananId, x.Layanan.LayananName,
                x.Ruang.RuangId, x.Ruang.RuangName})
            .Select(g => new JadwalPraktekListResponse(
                g.Key.PetugasMedisId,
                g.Key.PetugasMedisName,
                g.Key.LayananId,
                g.Key.LayananName,
                g.Key.RuangId,
                g.Key.RuangName,
                g.OrderBy(j => j.Hari).ThenBy(j => j.JamMulai)
                 .Select(j => new JadwalPraktekListHariResponse(
                     j.JadwalPraktekId,
                     j.Hari.ToString(),
                     j.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture),
                     j.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture),
                     j.MaxPasien
                 ))
            ));

        return Task.FromResult(result);
    }
}
