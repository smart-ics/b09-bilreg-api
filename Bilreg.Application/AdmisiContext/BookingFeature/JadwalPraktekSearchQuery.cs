using Ardalis.GuardClauses;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekSearchQuery(string Keyword) : IRequest<IEnumerable<JadwalPraktekSearchResponse>>;

public record JadwalPraktekSearchResponse(
    string DokterId, string DokterName,
    string LayananId, string LayananName,
    IEnumerable<JadwalPraktekSearchHariResponse> ListHari);

public record JadwalPraktekSearchHariResponse(
    string JadwalPraktekId, string Hari, string JamMulai, string JamSelesai);

public class JadwalPrektekSearchHandler : IRequestHandler<JadwalPraktekSearchQuery, IEnumerable<JadwalPraktekSearchResponse>>
{
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;

    public JadwalPrektekSearchHandler(IJadwalPraktekRepo jadwalPraktekRepo)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
    }

    public Task<IEnumerable<JadwalPraktekSearchResponse>> Handle(JadwalPraktekSearchQuery request, CancellationToken cancellationToken)
    {
        // Guard
        Guard.Against.NullOrWhiteSpace(request.Keyword, nameof(request.Keyword));

        var list = _jadwalPraktekRepo.ListData().ToList();
        if (list.Count == 0)
            return Task.FromResult(Enumerable.Empty<JadwalPraktekSearchResponse>());

        var tgl = ParseTanggal(request.Keyword);

        var filtered = tgl switch
        {
            not null => list.Where(x => x.Hari == tgl.Value.DayOfWeek),
            _ => list.Where(x => x.Dokter.PpaName.Contains(
                        request.Keyword, StringComparison.OrdinalIgnoreCase))
        };

        var result = filtered
            .GroupBy(x => new {
                PetugasMedisId = x.Dokter.PpaId,
                PetugasMedisName = x.Dokter.PpaName, 
                x.Layanan.LayananId, x.Layanan.LayananName })
            .Select(g => new JadwalPraktekSearchResponse(
                g.Key.PetugasMedisId,
                g.Key.PetugasMedisName,
                g.Key.LayananId,
                g.Key.LayananName,
                g.Select(x => new JadwalPraktekSearchHariResponse(
                    JadwalPraktekId: x.JadwalPraktekId,
                    Hari: x.Hari.ToString(),
                    JamMulai: x.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture),
                    JamSelesai: x.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture)
                ))
            ));

        return Task.FromResult(result);
    }

    private static DateTime? ParseTanggal(string keyword)
    {
        if (DateTime.TryParseExact(keyword, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;
        if (DateTime.TryParseExact(keyword, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            return parsed;
        return null;
    }
}