using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

public class JadwalPraktekResolver : IJadwalPraktekResolver
{
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IJadwalPraktekHarianRepo _jadwalPraktekHarianRepo;

    public JadwalPraktekResolver(
        IJadwalPraktekRepo jadwalPraktekRepo,
        IJadwalPraktekHarianRepo jadwalPraktekHarianRepo)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _jadwalPraktekHarianRepo = jadwalPraktekHarianRepo;
    }

    public JadwalPraktekEffective Resolve(JadwalPraktekResolveRequest request)
    {
        var dailyRows = _jadwalPraktekHarianRepo
            .ListByDateAndDokter(request.TglPraktek, request.Dokter)
            .ToList();

        var dailyMatch = ResolveDailyMatch(dailyRows, request.JamMulai);
        if (dailyMatch is not null)
        {
            if (dailyMatch.Status == JadwalPraktekScheduleStatus.CANCELLED)
            {
                if (request.Options.ThrowIfCancelled)
                    throw new ArgumentException("Jadwal praktek dibatalkan untuk tanggal ini");
                return JadwalPraktekEffectiveMapper.FromDaily(dailyMatch);
            }

            return JadwalPraktekEffectiveMapper.FromDaily(dailyMatch);
        }

        var templates = (_jadwalPraktekRepo.ListData(request.Dokter) ?? [])
            .Where(x => x.Hari == request.TglPraktek.DayOfWeek)
            .ToList();

        var templateMatch = ResolveTemplateMatch(templates, request.JamMulai);
        if (templateMatch is not null)
            return JadwalPraktekEffectiveMapper.FromTemplate(templateMatch, request.TglPraktek);

        if (request.Options.AllowSynthetic)
        {
            var jamMulai = request.JamMulai ?? TimeOnly.MinValue;
            var jamSelesai = request.JamMulai ?? new TimeOnly(23, 59);
            var dokter = LoadDokterReff(request.Dokter);
            return JadwalPraktekEffectiveMapper.Synthetic(request.TglPraktek, dokter, jamMulai, jamSelesai);
        }

        if (request.Options.ThrowIfNotFound)
            throw new ArgumentException("Jadwal tidak ditemukan");

        throw new InvalidOperationException("Resolve reached unreachable path");
    }

    public IEnumerable<JadwalPraktekEffective> ResolveForDate(JadwalPraktekResolveForDateRequest request)
    {
        var dailyRows = _jadwalPraktekHarianRepo.ListByDate(request.TglPraktek).ToList();
        if (request.DokterFilter is not null)
            dailyRows = dailyRows.Where(x => x.Dokter.PpaId == request.DokterFilter.PpaId).ToList();

        var activeDailies = dailyRows
            .Where(x => x.Status == JadwalPraktekScheduleStatus.ACTIVE)
            .ToList();

        var cancelledDailies = request.IncludeCancelled
            ? dailyRows.Where(x => x.Status == JadwalPraktekScheduleStatus.CANCELLED).ToList()
            : [];

        var templates = (_jadwalPraktekRepo.ListData() ?? [])
            .Where(x => x.Hari == request.TglPraktek.DayOfWeek)
            .ToList();

        if (request.DokterFilter is not null)
            templates = templates.Where(x => x.Dokter.PpaId == request.DokterFilter.PpaId).ToList();

        var results = new List<JadwalPraktekEffective>();
        var coveredKeys = new HashSet<(string DokterId, TimeOnly JamMulai)>();

        foreach (var daily in activeDailies)
        {
            var effective = JadwalPraktekEffectiveMapper.FromDaily(daily);
            var key = (daily.Dokter.PpaId, daily.JamMulai);
            if (coveredKeys.Add(key))
                results.Add(effective);
        }

        foreach (var template in templates)
        {
            var key = (template.Dokter.PpaId, template.JamMulai);
            if (coveredKeys.Contains(key))
                continue;

            var hasActiveDaily = activeDailies.Any(d =>
                d.Dokter.PpaId == template.Dokter.PpaId &&
                d.JamMulai == template.JamMulai);

            if (!hasActiveDaily)
            {
                results.Add(JadwalPraktekEffectiveMapper.FromTemplate(template, request.TglPraktek));
                coveredKeys.Add(key);
            }
        }

        foreach (var cancelled in cancelledDailies)
        {
            var key = (cancelled.Dokter.PpaId, cancelled.JamMulai);
            if (coveredKeys.Add(key))
                results.Add(JadwalPraktekEffectiveMapper.FromDaily(cancelled));
        }

        return results;
    }

    private static JadwalPraktekHarianType? ResolveDailyMatch(
        List<JadwalPraktekHarianType> dailyRows, TimeOnly? jamMulai)
    {
        if (dailyRows.Count == 0)
            return null;

        if (jamMulai.HasValue)
            return dailyRows.FirstOrDefault(x => x.JamMulai == jamMulai.Value);

        if (dailyRows.Count == 1)
            return dailyRows[0];

        throw new ArgumentException("JamMulai wajib diisi");
    }

    private static JadwalPraktekType? ResolveTemplateMatch(
        List<JadwalPraktekType> templates, TimeOnly? jamMulai)
    {
        if (templates.Count == 0)
            return null;

        if (jamMulai.HasValue)
            return templates.FirstOrDefault(x => x.JamMulai == jamMulai.Value);

        if (templates.Count == 1)
            return templates[0];

        throw new ArgumentException("JamMulai wajib diisi");
    }

    private static PpaReff LoadDokterReff(IPpaKey dokter)
    {
        if (dokter is PpaReff reff)
            return reff;
        return new PpaReff(dokter.PpaId, dokter.PpaId);
    }
}
