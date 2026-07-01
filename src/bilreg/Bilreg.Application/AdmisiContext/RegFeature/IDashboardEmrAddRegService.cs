using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IAddAntrianEmrByRegService : INunaServiceVoid<AddAntrianEmrByRegCommand>
{
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
