using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IAddAntrianEmrByBookingService : INunaServiceVoid<AddAntrianEmrByBookingCmd>
{
    EmrAntrianSendResult Send(AddAntrianEmrByBookingCmd cmd);
}

public record AddAntrianEmrByBookingCmd(string BookingId, string PasienId, string PasienName,
    string LayananId, string DokterId, string TglBerobat, string jamJadwal, int NoAntrian);