using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public class PublishTrackerOnPreOpClearedEvHandler : INotificationHandler<OkPreOpClearedEvent>
{
    private readonly IPasienTrackerRepo _pasienTrackerRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public PublishTrackerOnPreOpClearedEvHandler(
        IPasienTrackerRepo pasienTrackerRepo,
        ITglJamProvider tglJamProvider)
    {
        _pasienTrackerRepo = pasienTrackerRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(OkPreOpClearedEvent notification, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        var regId = notification.Clearance?.RegId ?? string.Empty;
        var clearanceId = notification.Clearance?.ClearanceId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(regId) || string.IsNullOrWhiteSpace(clearanceId))
            return Task.CompletedTask;

        // Primary lookup by RegId
        var trackerOpt = _pasienTrackerRepo.LoadEntity(PasienTrackerModel.Key(regId));
        PasienTrackerModel? tracker = trackerOpt.HasValue ? trackerOpt.Value : null;

        // Fallback: search tracker by period and patient's date of birth if not found directly
        if (tracker is null && notification.Pasien is not null && notification.Pasien.TglLahir != DateOnly.MinValue)
        {
            var opDate = notification.TglOp is { } tglOp && tglOp != DateTime.MinValue
                ? tglOp
                : occurredAt;
            var periode = new Periode(opDate.Date);
            var candidates = _pasienTrackerRepo.ListData(periode, notification.Pasien.TglLahir)?.ToList() ?? [];
            foreach (var cand in candidates)
            {
                var candTracker = _pasienTrackerRepo.LoadEntity(PasienTrackerModel.Key(cand.PasienTrackerId)).GetValueOrDefault();
                if (candTracker != null && candTracker.ListEvent.Any(e => e.ReffId == regId))
                {
                    tracker = candTracker;
                    break;
                }
            }
        }

        if (tracker is null)
            return Task.CompletedTask;

        tracker.AddEvent("PREOP_CLEARED", clearanceId, occurredAt);
        _pasienTrackerRepo.SaveChanges(tracker);

        return Task.CompletedTask;
    }
}
