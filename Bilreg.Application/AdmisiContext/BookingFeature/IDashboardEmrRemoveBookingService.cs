using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IDashboardEmrRemoveBookingService : INunaServiceVoid<RemoveBookingCmd>
{
}

public record RemoveBookingCmd(string BookingId);
