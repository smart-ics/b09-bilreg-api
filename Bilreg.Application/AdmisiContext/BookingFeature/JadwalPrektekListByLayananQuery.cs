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

        throw new NotImplementedException();
    }
}
