using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;

public record JadwalPraktekEffectiveResponse(
    string TglPraktek,
    string DokterId,
    string JamMulai,
    string JamSelesai,
    string LayananId,
    string RuangId,
    int MaxPasien,
    string Status,
    int AvailableQuota);

public record JadwalPraktekEffectiveListQuery(string DokterId, string TglYmd)
    : IRequest<IEnumerable<JadwalPraktekEffectiveResponse>>;

public class JadwalPraktekEffectiveListHandler
    : IRequestHandler<JadwalPraktekEffectiveListQuery, IEnumerable<JadwalPraktekEffectiveResponse>>
{
    private readonly IJadwalPraktekFeatureResolver _featureResolver;
    private readonly IAntrianRepo _antrianRepo;

    public JadwalPraktekEffectiveListHandler(
        IJadwalPraktekFeatureResolver featureResolver,
        IAntrianRepo antrianRepo)
    {
        _featureResolver = featureResolver;
        _antrianRepo = antrianRepo;
    }

    public Task<IEnumerable<JadwalPraktekEffectiveResponse>> Handle(
        JadwalPraktekEffectiveListQuery request, CancellationToken cancellationToken)
    {
        var tgl = DateOnly.ParseExact(request.TglYmd, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var dokter = PpaType.Key(request.DokterId);
        var sessions = _featureResolver.ResolveForDate(
            new JadwalPraktekResolveForDateRequest(tgl, DokterFilter: dokter));

        var result = sessions
            .Where(x => x.Status == JadwalPraktekScheduleStatus.ACTIVE)
            .Select(x => ToResponse(x, tgl));

        return Task.FromResult(result);
    }

    private JadwalPraktekEffectiveResponse ToResponse(JadwalPraktekEffective effective, DateOnly tgl)
    {
        var tag = Domain.AdmisiContext.AntrianFeature.AntrianModel.GenSequenceTag(tgl, effective);
        var antrianHeaders = _antrianRepo.ListData(tgl) ?? [];
        var header = antrianHeaders.FirstOrDefault(x => x.SequenceTag == tag);
        var used = header is null
            ? 0
            : _antrianRepo.LoadEntity(header).Value.ListEntry.Count();
        var available = Math.Max(0, effective.MaxPasien - used);

        return new JadwalPraktekEffectiveResponse(
            effective.TglPraktek.ToString("yyyy-MM-dd"),
            effective.Dokter.PpaId,
            effective.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture),
            effective.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture),
            effective.Layanan.LayananId,
            effective.Ruang.RuangId,
            effective.MaxPasien,
            effective.Status.ToString(),
            available);
    }
}

public record JadwalPraktekEffectiveGetQuery(string DokterId, string TglYmd, string JamMulai)
    : IRequest<JadwalPraktekEffectiveResponse>;

public class JadwalPraktekEffectiveGetHandler
    : IRequestHandler<JadwalPraktekEffectiveGetQuery, JadwalPraktekEffectiveResponse>
{
    private readonly IJadwalPraktekFeatureResolver _featureResolver;
    private readonly IAntrianRepo _antrianRepo;

    public JadwalPraktekEffectiveGetHandler(
        IJadwalPraktekFeatureResolver featureResolver,
        IAntrianRepo antrianRepo)
    {
        _featureResolver = featureResolver;
        _antrianRepo = antrianRepo;
    }

    public Task<JadwalPraktekEffectiveResponse> Handle(
        JadwalPraktekEffectiveGetQuery request, CancellationToken cancellationToken)
    {
        var tgl = DateOnly.ParseExact(request.TglYmd, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var jam = TimeOnly.ParseExact(request.JamMulai, "HH:mm", CultureInfo.InvariantCulture);
        var effective = _featureResolver.Resolve(new JadwalPraktekResolveRequest(
            tgl, PpaType.Key(request.DokterId), jam,
            new JadwalPraktekResolveOptions()));

        var tag = Domain.AdmisiContext.AntrianFeature.AntrianModel.GenSequenceTag(tgl, effective);
        var header = (_antrianRepo.ListData(tgl) ?? []).FirstOrDefault(x => x.SequenceTag == tag);
        var used = header is null
            ? 0
            : _antrianRepo.LoadEntity(header).Value.ListEntry.Count();

        var response = new JadwalPraktekEffectiveResponse(
            effective.TglPraktek.ToString("yyyy-MM-dd"),
            effective.Dokter.PpaId,
            effective.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture),
            effective.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture),
            effective.Layanan.LayananId,
            effective.Ruang.RuangId,
            effective.MaxPasien,
            effective.Status.ToString(),
            Math.Max(0, effective.MaxPasien - used));

        return Task.FromResult(response);
    }
}
