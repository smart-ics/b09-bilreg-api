using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;

public record JadwalPraktekHarianListQuery(string TglPraktek, string? DokterId = null)
    : IRequest<IEnumerable<JadwalPraktekHarianListItem>>;

public record JadwalPraktekHarianListItem(
    string JadwalPraktekHarianId,
    string? JadwalPraktekId,
    string TglPraktek,
    string DokterId,
    string DokterName,
    string JamMulai,
    string JamSelesai,
    int MaxPasien,
    string Status,
    string Source,
    string? Catatan);

public class JadwalPraktekHarianListHandler : IRequestHandler<JadwalPraktekHarianListQuery, IEnumerable<JadwalPraktekHarianListItem>>
{
    private readonly IJadwalPraktekHarianRepo _harianRepo;

    public JadwalPraktekHarianListHandler(IJadwalPraktekHarianRepo harianRepo)
    {
        _harianRepo = harianRepo;
    }

    public Task<IEnumerable<JadwalPraktekHarianListItem>> Handle(
        JadwalPraktekHarianListQuery request, CancellationToken cancellationToken)
    {
        var tgl = DateOnly.Parse(request.TglPraktek);
        var list = string.IsNullOrWhiteSpace(request.DokterId)
            ? _harianRepo.ListByDate(tgl)
            : _harianRepo.ListByDateAndDokter(tgl, PpaType.Key(request.DokterId));

        var result = list.Select(x => new JadwalPraktekHarianListItem(
            x.JadwalPraktekHarianId,
            x.JadwalPraktekId,
            x.TglPraktek.ToString("yyyy-MM-dd"),
            x.Dokter.PpaId,
            x.Dokter.PpaName,
            x.JamMulai.ToString("HH:mm"),
            x.JamSelesai.ToString("HH:mm"),
            x.MaxPasien,
            x.Status.ToString(),
            x.Source.ToString(),
            x.Catatan));

        return Task.FromResult(result);
    }
}
