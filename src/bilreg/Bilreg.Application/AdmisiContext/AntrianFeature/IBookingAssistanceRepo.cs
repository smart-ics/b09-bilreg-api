using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record BookingAssistanceActive(string BookingId, string AntrianId, int NoUrut, string? QueueLabel);

public interface IBookingAssistanceRepo
{
    BookingAssistanceActive? FindActive(string bookingId);
    BookingAssistanceActive? FindActiveByEntry(string antrianId, int noUrut);
    bool TryCreate(string bookingId, string correlation, string? failureCode, string kioskId, string userId,
        DateTime at, AntrianModel queue, AntrianEntryModel entry);
}
