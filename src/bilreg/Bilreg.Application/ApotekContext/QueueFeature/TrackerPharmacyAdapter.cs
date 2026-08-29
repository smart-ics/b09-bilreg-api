using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature;

public class TrackerPharmacyAdapter : ITrackerPharmacyPort
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPasienTrackerRepo _trackerRepo;

    public TrackerPharmacyAdapter(IAntrianRepo antrianRepo, IPasienTrackerRepo trackerRepo)
    {
        _antrianRepo = antrianRepo;
        _trackerRepo = trackerRepo;
    }

    public AntrianStatusEnum GetStatus(string antrianId, int noUrut)
        => Entry(antrianId, noUrut).AntrianStatus;

    public TrackerPharmacyCommandResult ServeOnce(
        string antrianId, int noUrut, string pasienTrackerId, string reffId, DateTime at)
    {
        var queue = Queue(antrianId);
        var entry = queue.ListEntry.First(x => x.NoUrut == noUrut);
        if (entry.AntrianStatus == AntrianStatusEnum.InService || entry.AntrianStatus == AntrianStatusEnum.Done)
        {
            AppendStart(pasienTrackerId, reffId, at);
            return new TrackerPharmacyCommandResult(false, entry.ServedAt.ToString("O"));
        }
        entry.Serve(at);
        if (!_antrianRepo.TrySaveWaitingToInServiceTransition(queue, entry))
            throw new Domain.ApotekContext.Shared.ApotekConcurrencyException($"{antrianId}:{noUrut}", 0);
        AppendStart(pasienTrackerId, reffId, at);
        return new TrackerPharmacyCommandResult(true, entry.ServedAt.ToString("O"));
    }

    public TrackerPharmacyCommandResult DoneOnce(
        string antrianId, int noUrut, string pasienTrackerId, string reffId, DateTime at)
    {
        var queue = Queue(antrianId);
        var entry = queue.ListEntry.First(x => x.NoUrut == noUrut);
        if (entry.AntrianStatus == AntrianStatusEnum.Done)
        {
            AppendDone(pasienTrackerId, reffId, at);
            return new TrackerPharmacyCommandResult(false, entry.DoneAt.ToString("O"));
        }
        if (entry.AntrianStatus == AntrianStatusEnum.InService)
        {
            entry.Done(at);
            if (!_antrianRepo.TrySaveInServiceToDoneTransition(queue, entry))
                throw new Domain.ApotekContext.Shared.ApotekConcurrencyException($"{antrianId}:{noUrut}", 0);
        }
        AppendDone(pasienTrackerId, reffId, at);
        return new TrackerPharmacyCommandResult(true, at.ToString("O"));
    }

    public TrackerPharmacyCommandResult WithdrawFromWaiting(
        string antrianId, int noUrut, string reason, string userId, DateTime at)
    {
        var queue = Queue(antrianId);
        var entry = queue.ListEntry.First(x => x.NoUrut == noUrut);
        if (entry.AntrianStatus != AntrianStatusEnum.Waiting)
            return new TrackerPharmacyCommandResult(false, "");
        entry.Withdraw(reason, userId, at);
        _antrianRepo.SaveChanges(queue);
        return new TrackerPharmacyCommandResult(true, entry.WithdrawnAt.ToString("O"));
    }

    private AntrianModel Queue(string antrianId)
        => _antrianRepo.LoadEntity(AntrianModel.Key(antrianId))
            .GetValueOrThrow($"Antrian '{antrianId}' not found");

    private AntrianEntryModel Entry(string antrianId, int noUrut)
        => Queue(antrianId).ListEntry.First(x => x.NoUrut == noUrut);

    private void AppendStart(string pasienTrackerId, string reffId, DateTime at)
    {
        if (!PasienTrackerStableIdentity.IsRealTrackerId(pasienTrackerId))
            return;
        var tracker = PharmacyQueueEvidence.RequireTracker(_trackerRepo, pasienTrackerId);
        PharmacyQueueEvidence.AppendApotekStart(tracker, reffId, at);
        _trackerRepo.SaveChanges(tracker);
    }

    private void AppendDone(string pasienTrackerId, string reffId, DateTime at)
    {
        if (!PasienTrackerStableIdentity.IsRealTrackerId(pasienTrackerId))
            return;
        var tracker = PharmacyQueueEvidence.RequireTracker(_trackerRepo, pasienTrackerId);
        PharmacyQueueEvidence.AppendApotekDone(tracker, reffId, at);
        _trackerRepo.SaveChanges(tracker);
    }
}
