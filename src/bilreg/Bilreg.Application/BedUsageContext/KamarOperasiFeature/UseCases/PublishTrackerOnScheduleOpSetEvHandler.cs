using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public class PublishTrackerOnScheduleOpSetEvHandler : INotificationHandler<OkScheduleOpSetEvent>
{
    private readonly IPasienTrackerRepo _pasienTrackerRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public PublishTrackerOnScheduleOpSetEvHandler(
        IPasienTrackerRepo pasienTrackerRepo,
        ITglJamProvider tglJamProvider)
    {
        _pasienTrackerRepo = pasienTrackerRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(OkScheduleOpSetEvent notification, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        var regId = notification.Aggregate?.Reg?.RegId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(regId))
            return Task.CompletedTask;

        // Try direct key lookup by RegId
        var trackerOpt = _pasienTrackerRepo.LoadEntity(PasienTrackerModel.Key(regId));
        PasienTrackerModel? tracker = trackerOpt.HasValue ? trackerOpt.Value : null;

        // Fallback: search tracker by period and patient's date of birth if not found directly
        if (tracker is null && notification.Aggregate?.Pasien is not null && notification.Aggregate.Pasien.TglLahir != DateOnly.MinValue)
        {
            var opDate = notification.Aggregate.TglOp != DateTime.MinValue ? notification.Aggregate.TglOp : occurredAt;
            var periode = new Periode(opDate.Date);
            var candidates = _pasienTrackerRepo.ListData(periode, notification.Aggregate.Pasien.TglLahir)?.ToList() ?? [];
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

        tracker.AddEvent("OPERASI_TERJADWAL", notification.Aggregate.ScheduleOpId, occurredAt);
        _pasienTrackerRepo.SaveChanges(tracker);

        return Task.CompletedTask;
    }
}
