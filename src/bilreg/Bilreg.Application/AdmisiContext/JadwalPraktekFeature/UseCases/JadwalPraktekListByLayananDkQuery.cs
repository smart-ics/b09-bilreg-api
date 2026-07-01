using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;

public record JadwalPraktekListByLayananDkQuery(string LayananDkId) : 
    IRequest<IEnumerable<JadwalPraktekListByLayananDkResponse>>, ILayananDkKey;

public record JadwalPraktekListByLayananDkResponse(
    string DokterId, string DokterName,
    string LayananId, string LayananName,
    string RuangId, string RuangName,
    IEnumerable<JadwalPraktekDokterByLynDkHariResponse> ListHari);

public record JadwalPraktekDokterByLynDkHariResponse(
    string JadwalPraktekId, string Hari, string JamMulai, string JamSelesai, int MaxPasien);

public class JadwalPraktekListByLayananDkHandler : IRequestHandler<JadwalPraktekListByLayananDkQuery,
    IEnumerable<JadwalPraktekListByLayananDkResponse>>
{
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;

    public JadwalPraktekListByLayananDkHandler(IJadwalPraktekRepo jadwalPraktekRepo)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
    }

    public Task<IEnumerable<JadwalPraktekListByLayananDkResponse>> Handle(JadwalPraktekListByLayananDkQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananDkId, nameof(request.LayananDkId));

        var listJadwal = _jadwalPraktekRepo
            .ListData(LayananDkType.Key(request.LayananDkId))?.ToList() ?? [];

        var result = listJadwal
            .GroupBy(x => new {
                PetugasMedisId = x.Dokter.PpaId,
                PetugasMedisName = x.Dokter.PpaName,
                x.Layanan.LayananId,
                x.Layanan.LayananName,
                x.Ruang.RuangId,
                x.Ruang.RuangName
            })
            .Select(g => new JadwalPraktekListByLayananDkResponse(
                g.Key.PetugasMedisId,
                g.Key.PetugasMedisName,
                g.Key.LayananId,
                g.Key.LayananName,
                g.Key.RuangId,
                g.Key.RuangName,
                g.OrderBy(j => j.Hari).ThenBy(j => j.JamMulai)
                 .Select(j => new JadwalPraktekDokterByLynDkHariResponse(
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