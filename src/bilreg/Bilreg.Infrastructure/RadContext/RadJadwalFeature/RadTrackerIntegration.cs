using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.RadContext.RadJadwalFeature;

public class RadTrackerIntegration : IRadTrackerIntegration
{
    private const string EventPrefix = "RAD_";
    private const int MaxEventNameLength = 50;
    private const int MaxReffIdLength = 40;

    private readonly IPasienTrackerRepo _pasienTrackerRepo;
    private readonly IRegRepo _regRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILogger<RadTrackerIntegration> _logger;

    public RadTrackerIntegration(
        IPasienTrackerRepo pasienTrackerRepo,
        IRegRepo regRepo,
        ITglJamProvider tglJamProvider,
        ILogger<RadTrackerIntegration>? logger = null)
    {
        _pasienTrackerRepo = pasienTrackerRepo;
        _regRepo = regRepo;
        _tglJamProvider = tglJamProvider;
        _logger = logger ?? NullLogger<RadTrackerIntegration>.Instance;
    }

    public Task PublishMilestoneAsync(
        string regId,
        string jadwalRadId,
        string milestone,
        string note,
        CancellationToken ct = default)
    {
        try
        {
            if (ct.IsCancellationRequested)
                return Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(regId) || regId.Trim() == "-")
            {
                _logger.LogDebug(
                    "Skipping radiology tracker milestone {Milestone} for JadwalRadId {JadwalRadId}: RegId is empty.",
                    milestone, jadwalRadId);
                return Task.CompletedTask;
            }

            if (string.IsNullOrWhiteSpace(milestone))
            {
                _logger.LogWarning(
                    "Skipping radiology tracker update for RegId {RegId}, JadwalRadId {JadwalRadId}: Milestone is empty.",
                    regId, jadwalRadId);
                return Task.CompletedTask;
            }

            var normalizedRegId = regId.Trim();
            var tracker = ResolveTracker(normalizedRegId);
            if (tracker is null)
            {
                _logger.LogDebug(
                    "PasienTracker not found for RegId {RegId}; skipping radiology milestone {Milestone} (JadwalRadId {JadwalRadId}).",
                    normalizedRegId, milestone, jadwalRadId);
                return Task.CompletedTask;
            }

            var occurredAt = _tglJamProvider.Now;
            var eventName = FormatEventName(milestone);
            var reffId = FormatReffId(string.IsNullOrWhiteSpace(jadwalRadId) ? normalizedRegId : jadwalRadId);

            tracker.AddEvent(eventName, reffId, occurredAt);
            _pasienTrackerRepo.SaveChanges(tracker);

            _logger.LogInformation(
                "Published radiology tracker milestone {EventName} for RegId {RegId}, JadwalRadId {JadwalRadId}, PasienTrackerId {PasienTrackerId}. Note: {Note}",
                eventName, normalizedRegId, jadwalRadId, tracker.PasienTrackerId, note);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish radiology tracker milestone {Milestone} for RegId {RegId}, JadwalRadId {JadwalRadId}. Main scheduling transaction is unaffected.",
                milestone, regId, jadwalRadId);
        }

        return Task.CompletedTask;
    }

    private PasienTrackerModel? ResolveTracker(string regId)
    {
        var directOpt = _pasienTrackerRepo.LoadEntity(PasienTrackerModel.Key(regId));
        if (directOpt.HasValue)
            return directOpt.Value;

        var regOpt = _regRepo.LoadEntity(RegModel.Key(regId));
        if (!regOpt.HasValue)
            return null;

        var reg = regOpt.Value;
        if (reg.Pasien.TglLahir == DateOnly.MinValue)
            return null;

        var regDate = reg.RegDate.ToDateTime(TimeOnly.MinValue);
        var today = _tglJamProvider.Now.Date;
        var periode = today >= regDate
            ? new Periode(regDate, today)
            : new Periode(regDate);

        var candidates = _pasienTrackerRepo.ListData(periode, reg.Pasien.TglLahir)?.ToList() ?? [];
        foreach (var cand in candidates)
        {
            var candOpt = _pasienTrackerRepo.LoadEntity(PasienTrackerModel.Key(cand.PasienTrackerId));
            if (candOpt.HasValue && candOpt.Value.ListEvent.Any(e => string.Equals(e.ReffId, regId, StringComparison.OrdinalIgnoreCase)))
                return candOpt.Value;
        }

        return null;
    }

    private static string FormatEventName(string milestone)
    {
        var normalized = milestone.Trim().ToUpperInvariant().Replace(' ', '_');
        if (!normalized.StartsWith(EventPrefix, StringComparison.Ordinal))
            normalized = $"{EventPrefix}{normalized}";

        return normalized.Length > MaxEventNameLength
            ? normalized[..MaxEventNameLength]
            : normalized;
    }

    private static string FormatReffId(string reffId)
    {
        var normalized = reffId.Trim();
        return normalized.Length > MaxReffIdLength
            ? normalized[..MaxReffIdLength]
            : normalized;
    }
}
