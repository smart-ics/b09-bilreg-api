using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IDashboardAddBookService : INunaServiceVoid<AddBookCmd>
{
}

public record AddBookCmd(string bookingId);