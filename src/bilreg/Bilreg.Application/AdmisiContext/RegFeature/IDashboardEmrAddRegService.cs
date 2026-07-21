using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IAddAntrianEmrByRegService : INunaServiceVoid<AddAntrianEmrByRegCommand>
{
    EmrAntrianSendResult Send(AddAntrianEmrByRegCommand cmd);
}

public record AddAntrianEmrByRegCommand
(
    string RegId,
    string BookingId,
    string PasienId,
    string PasienName,
    string LayananId,
    string DokterId,
    string TglBerobat,
    string JamJadwal,
    int NoAntrian
);
