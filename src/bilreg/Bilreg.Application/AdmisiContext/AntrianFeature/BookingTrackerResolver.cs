using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>Finds the stable journey identity created with a Booking's physician queue entry.</summary>
internal static class BookingTrackerResolver
{
    public static (AntrianModel Queue, PasienTrackerModel Tracker) Resolve(
        IAntrianRepo queues, IPpaRepo ppas, IPasienTrackerRepo trackers, BookingModel booking)
    {
        var ppa = ppas.LoadEntity(PpaType.Key(booking.Dokter.PpaId))
            .GetValueOrThrow($"Dokter '{booking.Dokter.PpaId}' not found");
        var sequenceTag = AntrianModel.GenSequenceTag(booking.TglBerobat, booking.JamPraktek, ppa);
        var header = queues.ListData(booking.TglBerobat)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag)
            ?? throw new KeyNotFoundException($"Booking queue for '{booking.BookingId}' not found");
        var queue = queues.LoadEntity(header).GetValueOrThrow($"Booking queue '{header.AntrianId}' not found");
        var entry = queue.ListEntry.FirstOrDefault(x => x.NoUrut == booking.NoAntrian)
            ?? throw new KeyNotFoundException($"Booking queue entry for '{booking.BookingId}' not found");
        var tracker = trackers.LoadEntity(entry.Tracker)
            .GetValueOrThrow($"PasienTracker '{entry.Tracker.PasienTrackerId}' not found");
        return (queue, tracker);
    }
}
