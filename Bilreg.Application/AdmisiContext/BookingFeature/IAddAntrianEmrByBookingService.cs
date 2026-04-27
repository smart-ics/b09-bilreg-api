using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IAddAntrianEmrByBookingService : INunaServiceVoid<AddAntrianEmrByBookingCmd>
{
}

public record AddAntrianEmrByBookingCmd(string BookingId, string PasienId, string PasienName,
    string LayananId, string DokterId, string TglBerobat, string jamJadwal, int NoAntrian);