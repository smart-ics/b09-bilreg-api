using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IDashboardAddBookService : INunaServiceVoid<AddBookCmd>
{
}

public record AddBookCmd(string BookingId, string PasienId, string PasienName,
    string LayananId, string DokterId, string TglBerobat, string jamJadwal, int NoAntrian);